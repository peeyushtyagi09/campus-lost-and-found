using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using Testcontainers.MongoDb;

namespace LostAndFound.Api.Tests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MongoDbContainer? _container;
    private MongoDbRunner? _mongoRunner;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new MongoDbBuilder("mongo:7")
                .Build();
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }
        catch (Exception ex) when (IsDockerUnavailable(ex))
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
                _container = null;
            }

            _mongoRunner = MongoDbRunner.Start();
            _connectionString = _mongoRunner.ConnectionString;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        _mongoRunner?.Dispose();
    }

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = _connectionString,
                ["Mongo:DatabaseName"] = "lostandfound_tests",
                ["Jwt:Issuer"] = "campus-lost-and-found-tests",
                ["Jwt:Audience"] = "campus-lost-and-found-tests",
                ["Jwt:SigningKey"] = new string('k', 48),
                ["Jwt:ExpiryMinutes"] = "60",
                ["Encryption:Key"] = Convert.ToBase64String(Enumerable.Repeat((byte)7, 32).ToArray()),
                ["Storage:UploadPath"] = Path.Combine(Path.GetTempPath(), "lost-and-found-tests"),
                ["Storage:MaxPhotoBytes"] = "5242880",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["RateLimiting:AuthPermitLimit"] = "500",
                ["RateLimiting:UploadPermitLimit"] = "500",
                ["RateLimiting:GlobalPermitLimit"] = "1000",
                ["RateLimiting:WindowSeconds"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TestDiagnosticsController).Assembly);
        });
    }

    private static bool IsDockerUnavailable(Exception exception)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return exception.GetType().Name.Contains("Docker", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("Docker", StringComparison.OrdinalIgnoreCase)
            || exception.InnerException is not null && IsDockerUnavailable(exception.InnerException);
    }
}

[CollectionDefinition(ApiCollection.Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
