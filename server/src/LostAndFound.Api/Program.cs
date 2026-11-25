using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Middleware;
using LostAndFound.Api.Options;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationActionFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Campus Lost & Found API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer authorization header. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((jwtBearerOptions, jwtOptionsAccessor) =>
    {
        var jwt = jwtOptionsAccessor.Value;
        jwtBearerOptions.MapInboundClaims = false;
        jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.RequireStaff, policy => policy.RequireRole(nameof(UserRole.Staff)));
    options.AddPolicy(AuthPolicies.RequireAdmin, policy => policy.RequireRole(nameof(UserRole.Admin)));
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

AddValidatedOptions<MongoOptions, MongoOptionsValidator>(builder, MongoOptions.SectionName);
AddValidatedOptions<JwtOptions, JwtOptionsValidator>(builder, JwtOptions.SectionName);
AddValidatedOptions<EncryptionOptions, EncryptionOptionsValidator>(builder, EncryptionOptions.SectionName);
AddValidatedOptions<StorageOptions, StorageOptionsValidator>(builder, StorageOptions.SectionName);
AddValidatedOptions<CorsOptions, CorsOptionsValidator>(builder, CorsOptions.SectionName);
AddValidatedOptions<RateLimitOptions, RateLimitOptionsValidator>(builder, RateLimitOptions.SectionName);

var storage = builder.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = storage.MaxPhotoBytes;
});
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxRequestBodySize = storage.MaxPhotoBytes;
});

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var mongoOptions = serviceProvider.GetRequiredService<IOptions<MongoOptions>>().Value;
    var settings = MongoClientSettings.FromConnectionString(mongoOptions.ConnectionString);
    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
    return new MongoClient(settings);
});
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddHostedService<MongoIndexHostedService>();

builder.Services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
builder.Services.AddSingleton<IEmailHasher, HmacEmailHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPhotoSanitizer, ImageSharpPhotoSanitizer>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IUserRepository, MongoUserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILostReportRepository, MongoLostReportRepository>();
builder.Services.AddScoped<ILostReportService, LostReportService>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddMongoDb(
        clientFactory: serviceProvider => serviceProvider.GetRequiredService<IMongoClient>(),
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(5),
        tags: ["ready"]);

builder.Services.AddCors();
builder.Services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>()
    .Configure<IOptions<CorsOptions>>((cors, appCorsOptions) =>
    {
        var origins = appCorsOptions.Value.AllowedOrigins;
        cors.AddPolicy(CorsOptions.PolicyName, policy =>
        {
            if (origins.Length > 0)
            {
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
            }
        });
    });

builder.Services.AddRateLimiter(rateLimiter =>
{
    rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    rateLimiter.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Type = "https://tools.ietf.org/html/rfc7807"
        };
        await context.HttpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };

    rateLimiter.AddPolicy(RateLimitOptions.AuthPolicy, httpContext =>
    {
        var limits = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        return FixedWindow(httpContext, limits.AuthPermitLimit, TimeSpan.FromSeconds(Math.Max(limits.WindowSeconds, 1)));
    });
    rateLimiter.AddPolicy(RateLimitOptions.UploadPolicy, httpContext =>
    {
        var limits = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        return FixedWindow(httpContext, limits.UploadPermitLimit, TimeSpan.FromSeconds(Math.Max(limits.WindowSeconds, 1)));
    });
    rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        var limits = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        return FixedWindow(httpContext, limits.GlobalPermitLimit, TimeSpan.FromSeconds(Math.Max(limits.WindowSeconds, 1)));
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();
app.UseRateLimiter();
app.UseCors(CorsOptions.PolicyName);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

app.Run();

static void AddValidatedOptions<TOptions, TValidator>(WebApplicationBuilder builder, string sectionName)
    where TOptions : class
    where TValidator : class, IValidateOptions<TOptions>
{
    builder.Services.AddSingleton<IValidateOptions<TOptions>, TValidator>();
    builder.Services.AddOptions<TOptions>()
        .Bind(builder.Configuration.GetSection(sectionName))
        .ValidateOnStart();
}

static RateLimitPartition<string> FixedWindow(HttpContext httpContext, int permitLimit, TimeSpan window)
{
    var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(
        key,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(permitLimit, 1),
            Window = window,
            QueueLimit = 0
        });
}

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    var payload = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString()
        })
    };
    return context.Response.WriteAsJsonAsync(payload);
}

public partial class Program;
