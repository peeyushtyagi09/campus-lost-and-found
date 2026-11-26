using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

public interface ILostReportRepository
{
    Task CreateAsync(LostReport report, CancellationToken cancellationToken);

    Task<LostReport?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<LostReport> Items, long TotalCount)> GetByReporterAsync(
        ObjectId reporterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<LostReport> Items, long TotalCount)> SearchPublicAsync(
        ItemCategory? category,
        string? campusArea,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LostReport>> GetOpenByCategoryAsync(ItemCategory category, CancellationToken cancellationToken);

    Task UpdateAsync(LostReport report, CancellationToken cancellationToken);

    Task DeleteAsync(ObjectId id, CancellationToken cancellationToken);

    Task<bool> IsPhotoReferencedAsync(
        string photoFileName,
        ObjectId excludingReportId,
        CancellationToken cancellationToken);
}
