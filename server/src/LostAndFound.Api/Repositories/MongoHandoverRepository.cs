using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoHandoverRepository : IHandoverRepository
{
    private readonly IMongoCollection<Handover> _handovers;

    public MongoHandoverRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _handovers = context.Handovers;
    }

    public async Task CreateAsync(Handover handover, CancellationToken cancellationToken)
    {
        await _handovers.InsertOneAsync(handover, cancellationToken: cancellationToken);
    }

    public async Task<Handover?> GetByClaimIdAsync(ObjectId claimId, CancellationToken cancellationToken)
    {
        return await _handovers.Find(h => h.ClaimId == claimId).FirstOrDefaultAsync(cancellationToken);
    }
}
