using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

public interface IClaimRepository
{
    Task CreateAsync(Claim claim, CancellationToken cancellationToken);

    Task<Claim?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Claim>> GetByClaimantAsync(ObjectId claimantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Claim>> GetByFoundReportAsync(ObjectId foundReportId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Claim>> GetForReviewQueueAsync(ClaimStatus? status, CancellationToken cancellationToken);

    Task UpdateAsync(Claim claim, CancellationToken cancellationToken);
}
