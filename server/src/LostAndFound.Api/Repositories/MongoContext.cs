using LostAndFound.Api.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
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

    public IMongoCollection<BsonDocument> Users => _database.GetCollection<BsonDocument>(UsersCollection);

    public IMongoCollection<BsonDocument> LostReports => _database.GetCollection<BsonDocument>(LostReportsCollection);
}

internal sealed class MongoIndexHostedService : IHostedService
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

        var emailHash = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("EmailHash"),
            new CreateIndexOptions { Name = "ux_users_email_hash", Unique = true });
        await _context.Users.Indexes.CreateOneAsync(emailHash, cancellationToken: cancellationToken);

        var lostReportIndexes = new[]
        {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending("Status")
                    .Ascending("Category")
                    .Descending("CreatedAt"),
                new CreateIndexOptions { Name = "ix_lost_status_category_created" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending("ReporterId")
                    .Descending("CreatedAt"),
                new CreateIndexOptions { Name = "ix_lost_reporter_created" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Text("Title").Text("PublicDescription"),
                new CreateIndexOptions { Name = "tx_lost_title_description" })
        };

        await _context.LostReports.Indexes.CreateManyAsync(lostReportIndexes, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
