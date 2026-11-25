using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

public interface IHandoverRepository
{
    Task CreateAsync(Handover handover, CancellationToken cancellationToken);

    Task<Handover?> GetByClaimIdAsync(ObjectId claimId, CancellationToken cancellationToken);
}
