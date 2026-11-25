using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoClaimRepository : IClaimRepository
{
    private readonly IMongoCollection<Claim> _claims;

    public MongoClaimRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _claims = context.Claims;
    }

    public async Task CreateAsync(Claim claim, CancellationToken cancellationToken)
    {
        await _claims.InsertOneAsync(claim, cancellationToken: cancellationToken);
    }

    public async Task<Claim?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken)
    {
        return await _claims.Find(c => c.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Claim>> GetByClaimantAsync(
        ObjectId claimantId,
        CancellationToken cancellationToken)
    {
        return await _claims
            .Find(c => c.ClaimantId == claimantId)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Claim>> GetByFoundReportAsync(
        ObjectId foundReportId,
        CancellationToken cancellationToken)
    {
        return await _claims
            .Find(c => c.FoundReportId == foundReportId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Claim>> GetForReviewQueueAsync(
        ClaimStatus? status,
        CancellationToken cancellationToken)
    {
        var filter = status.HasValue
            ? Builders<Claim>.Filter.Eq(c => c.Status, status.Value)
            : Builders<Claim>.Filter.Empty;

        return await _claims
            .Find(filter)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Claim claim, CancellationToken cancellationToken)
    {
        await _claims.ReplaceOneAsync(c => c.Id == claim.Id, claim, cancellationToken: cancellationToken);
    }
}
