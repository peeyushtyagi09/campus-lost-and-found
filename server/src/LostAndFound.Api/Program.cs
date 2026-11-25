using System.Threading.RateLimiting;
using FluentValidation;
using LostAndFound.Api.Middleware;
using LostAndFound.Api.Options;
using LostAndFound.Api.Repositories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAuthorization();
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

    var rateLimits = builder.Configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();
    var window = TimeSpan.FromSeconds(Math.Max(rateLimits.WindowSeconds, 1));

    rateLimiter.AddPolicy(RateLimitOptions.AuthPolicy, httpContext => FixedWindow(httpContext, rateLimits.AuthPermitLimit, window));
    rateLimiter.AddPolicy(RateLimitOptions.UploadPolicy, httpContext => FixedWindow(httpContext, rateLimits.UploadPermitLimit, window));
    rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        return FixedWindow(httpContext, rateLimits.GlobalPermitLimit, window);
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
