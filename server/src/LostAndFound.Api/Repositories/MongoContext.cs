using LostAndFound.Api.Entities;
using LostAndFound.Api.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoContext
{
    public const string UsersCollection = "users";
    public const string LostReportsCollection = "lost_reports";
    public const string FoundReportsCollection = "found_reports";
    public const string MatchesCollection = "matches";

    private readonly IMongoDatabase _database;

    public MongoContext(IMongoClient client, IOptions<MongoOptions> options)
    {
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>(UsersCollection);

    public IMongoCollection<LostReport> LostReports => _database.GetCollection<LostReport>(LostReportsCollection);

    public IMongoCollection<FoundReport> FoundReports => _database.GetCollection<FoundReport>(FoundReportsCollection);

    public IMongoCollection<ItemMatch> Matches => _database.GetCollection<ItemMatch>(MatchesCollection);
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

        var foundReportIndexes = new[]
        {
            new CreateIndexModel<FoundReport>(
                Builders<FoundReport>.IndexKeys
                    .Ascending(report => report.Status)
                    .Ascending(report => report.Category)
                    .Descending(report => report.CreatedAt),
                new CreateIndexOptions { Name = "ix_found_status_category_created" }),
            new CreateIndexModel<FoundReport>(
                Builders<FoundReport>.IndexKeys
                    .Ascending(report => report.FinderId)
                    .Descending(report => report.CreatedAt),
                new CreateIndexOptions { Name = "ix_found_finder_created" }),
            new CreateIndexModel<FoundReport>(
                Builders<FoundReport>.IndexKeys
                    .Text(report => report.Title)
                    .Text(report => report.PublicDescription),
                new CreateIndexOptions { Name = "tx_found_title_description" })
        };
        await _context.FoundReports.Indexes.CreateManyAsync(foundReportIndexes, cancellationToken);

        var matchIndexes = new[]
        {
            new CreateIndexModel<ItemMatch>(
                Builders<ItemMatch>.IndexKeys
                    .Ascending(m => m.LostReportId)
                    .Ascending(m => m.FoundReportId),
                new CreateIndexOptions { Name = "ux_matches_lost_found", Unique = true }),
            new CreateIndexModel<ItemMatch>(
                Builders<ItemMatch>.IndexKeys
                    .Ascending(m => m.LostReporterId)
                    .Ascending(m => m.Status)
                    .Descending(m => m.Score),
                new CreateIndexOptions { Name = "ix_matches_reporter_status_score" })
        };
        await _context.Matches.Indexes.CreateManyAsync(matchIndexes, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
