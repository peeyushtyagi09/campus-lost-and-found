using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

public interface IFoundReportRepository
{
    Task CreateAsync(FoundReport report, CancellationToken cancellationToken);

    Task<FoundReport?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<FoundReport>> GetByIdsAsync(IEnumerable<ObjectId> ids, CancellationToken cancellationToken);

    Task<(IReadOnlyList<FoundReport> Items, long TotalCount)> GetByFinderAsync(
        ObjectId finderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<FoundReport> Items, long TotalCount)> SearchPublicAsync(
        ItemCategory? category,
        string? campusArea,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FoundReport>> GetOpenByCategoryAsync(ItemCategory category, CancellationToken cancellationToken);

    Task<long> FlagEligibleForDisposalAsync(DateTime createdBeforeUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<FoundReport>> GetEligibleForDisposalAsync(CancellationToken cancellationToken);

    Task UpdateAsync(FoundReport report, CancellationToken cancellationToken);
}
