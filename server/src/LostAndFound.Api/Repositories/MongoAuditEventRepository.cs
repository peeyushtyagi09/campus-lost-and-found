using LostAndFound.Api.Entities;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoAuditEventRepository : IAuditEventRepository
{
    private readonly IMongoCollection<AuditEvent> _audit;

    public MongoAuditEventRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _audit = context.AuditEvents;
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await _audit.InsertOneAsync(auditEvent, cancellationToken: cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditEvent> Items, long TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await _audit.CountDocumentsAsync(Builders<AuditEvent>.Filter.Empty, cancellationToken: cancellationToken);
        var items = await _audit
            .Find(Builders<AuditEvent>.Filter.Empty)
            .SortByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
