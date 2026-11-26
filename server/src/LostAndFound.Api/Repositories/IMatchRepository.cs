using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

public interface IMatchRepository
{
    Task UpsertSuggestedAsync(ItemMatch match, CancellationToken cancellationToken);

    Task<ItemMatch?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItemMatch>> GetByReporterAsync(ObjectId lostReporterId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItemMatch>> GetAllAsync(CancellationToken cancellationToken);

    Task UpdateAsync(ItemMatch match, CancellationToken cancellationToken);

    Task DeleteByLostReportAsync(ObjectId lostReportId, CancellationToken cancellationToken);
}
