using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoCustodyEventRepository : ICustodyEventRepository
{
    private readonly IMongoCollection<CustodyEvent> _events;

    public MongoCustodyEventRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _events = context.CustodyEvents;
    }

    public async Task AppendAsync(CustodyEvent custodyEvent, CancellationToken cancellationToken)
    {
        await _events.InsertOneAsync(custodyEvent, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<CustodyEvent>> GetByReportIdAsync(
        ObjectId reportId,
        CancellationToken cancellationToken)
    {
        return await _events
            .Find(e => e.ReportId == reportId)
            .SortBy(e => e.SequenceNumber)
            .ToListAsync(cancellationToken);
    }
}
