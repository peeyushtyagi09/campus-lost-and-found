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
    public const string ClaimsCollection = "claims";
    public const string HandoversCollection = "handovers";
    public const string CustodyEventsCollection = "custody_events";
    public const string DisposalsCollection = "disposals";
    public const string NotificationsCollection = "notifications";
    public const string AuditEventsCollection = "audit_events";

    private readonly IMongoDatabase _database;

    public MongoContext(IMongoClient client, IOptions<MongoOptions> options)
    {
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>(UsersCollection);

    public IMongoCollection<LostReport> LostReports => _database.GetCollection<LostReport>(LostReportsCollection);

    public IMongoCollection<FoundReport> FoundReports => _database.GetCollection<FoundReport>(FoundReportsCollection);

    public IMongoCollection<ItemMatch> Matches => _database.GetCollection<ItemMatch>(MatchesCollection);

    public IMongoCollection<Claim> Claims => _database.GetCollection<Claim>(ClaimsCollection);

    public IMongoCollection<Handover> Handovers => _database.GetCollection<Handover>(HandoversCollection);

    public IMongoCollection<CustodyEvent> CustodyEvents => _database.GetCollection<CustodyEvent>(CustodyEventsCollection);

    public IMongoCollection<Disposal> Disposals => _database.GetCollection<Disposal>(DisposalsCollection);

    public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>(NotificationsCollection);

    public IMongoCollection<AuditEvent> AuditEvents => _database.GetCollection<AuditEvent>(AuditEventsCollection);
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

        var claimIndexes = new[]
        {
            new CreateIndexModel<Claim>(
                Builders<Claim>.IndexKeys
                    .Ascending(c => c.ClaimantId)
                    .Descending(c => c.CreatedAt),
                new CreateIndexOptions { Name = "ix_claims_claimant_created" }),
            new CreateIndexModel<Claim>(
                Builders<Claim>.IndexKeys
                    .Ascending(c => c.Status)
                    .Descending(c => c.CreatedAt),
                new CreateIndexOptions { Name = "ix_claims_status_created" }),
            new CreateIndexModel<Claim>(
                Builders<Claim>.IndexKeys.Ascending(c => c.FoundReportId),
                new CreateIndexOptions { Name = "ix_claims_found_report" })
        };
        await _context.Claims.Indexes.CreateManyAsync(claimIndexes, cancellationToken);

        var custodyIndex = new CreateIndexModel<CustodyEvent>(
            Builders<CustodyEvent>.IndexKeys
                .Ascending(e => e.ReportId)
                .Ascending(e => e.SequenceNumber),
            new CreateIndexOptions { Name = "ux_custody_report_seq", Unique = true });
        await _context.CustodyEvents.Indexes.CreateOneAsync(custodyIndex, cancellationToken: cancellationToken);

        var handoverIndex = new CreateIndexModel<Handover>(
            Builders<Handover>.IndexKeys.Ascending(h => h.ClaimId),
            new CreateIndexOptions { Name = "ux_handovers_claim", Unique = true });
        await _context.Handovers.Indexes.CreateOneAsync(handoverIndex, cancellationToken: cancellationToken);

        var disposalIndex = new CreateIndexModel<Disposal>(
            Builders<Disposal>.IndexKeys.Ascending(d => d.FoundReportId),
            new CreateIndexOptions { Name = "ux_disposals_found_report", Unique = true });
        await _context.Disposals.Indexes.CreateOneAsync(disposalIndex, cancellationToken: cancellationToken);

        var notificationIndex = new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys
                .Ascending(n => n.RecipientUserId)
                .Ascending(n => n.IsRead)
                .Descending(n => n.CreatedAt),
            new CreateIndexOptions { Name = "ix_notifications_recipient_read_created" });
        await _context.Notifications.Indexes.CreateOneAsync(notificationIndex, cancellationToken: cancellationToken);

        var auditIndex = new CreateIndexModel<AuditEvent>(
            Builders<AuditEvent>.IndexKeys.Descending(a => a.Timestamp),
            new CreateIndexOptions { Name = "ix_audit_timestamp_desc" });
        await _context.AuditEvents.Indexes.CreateOneAsync(auditIndex, cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
