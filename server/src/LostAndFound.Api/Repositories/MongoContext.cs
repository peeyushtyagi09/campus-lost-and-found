using LostAndFound.Api.Entities;
using LostAndFound.Api.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoContext
{
    public const string UsersCollection = "users";

    public const string LostReportsCollection = "lost_reports";

    private readonly IMongoDatabase _database;

    public MongoContext(IMongoClient client, IOptions<MongoOptions> options)
    {
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>(UsersCollection);

    public IMongoCollection<LostReport> LostReports => _database.GetCollection<LostReport>(LostReportsCollection);
}

public sealed class MongoIndexHostedService : IHostedService
{
    private readonly MongoContext _context;
    private readonly ILogger<MongoIndexHostedService> _logger;

    public MongoIndexHostedService(MongoContext context, ILogger<MongoIndexHostedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ensuring MongoDB indexes exist.");

        var emailHash = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(user => user.EmailHash),
            new CreateIndexOptions { Name = "ux_users_email_hash", Unique = true });
        await _context.Users.Indexes.CreateOneAsync(emailHash, cancellationToken: cancellationToken);

        var lostReportIndexes = new[]
        {
            new CreateIndexModel<LostReport>(
                Builders<LostReport>.IndexKeys
                    .Ascending(report => report.Status)
                    .Ascending(report => report.Category)
                    .Descending(report => report.CreatedAt),
                new CreateIndexOptions { Name = "ix_lost_status_category_created" }),
            new CreateIndexModel<LostReport>(
                Builders<LostReport>.IndexKeys
                    .Ascending(report => report.ReporterId)
                    .Descending(report => report.CreatedAt),
                new CreateIndexOptions { Name = "ix_lost_reporter_created" }),
            new CreateIndexModel<LostReport>(
                Builders<LostReport>.IndexKeys
                    .Text(report => report.Title)
                    .Text(report => report.PublicDescription),
                new CreateIndexOptions { Name = "tx_lost_title_description" })
        };

        await _context.LostReports.Indexes.CreateManyAsync(lostReportIndexes, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
