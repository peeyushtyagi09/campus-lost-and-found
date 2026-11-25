using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

/// <summary>
/// Append-only custody event store. Exposes insert and read operations only.
/// </summary>
public interface ICustodyEventRepository
{
    Task AppendAsync(CustodyEvent custodyEvent, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustodyEvent>> GetByReportIdAsync(ObjectId reportId, CancellationToken cancellationToken);
}
