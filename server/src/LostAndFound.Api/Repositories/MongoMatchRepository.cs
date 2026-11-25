using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoMatchRepository : IMatchRepository
{
    private readonly IMongoCollection<ItemMatch> _matches;

    public MongoMatchRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _matches = context.Matches;
    }

    public async Task UpsertSuggestedAsync(ItemMatch match, CancellationToken cancellationToken)
    {
        var filter = Builders<ItemMatch>.Filter.And(
            Builders<ItemMatch>.Filter.Eq(m => m.LostReportId, match.LostReportId),
            Builders<ItemMatch>.Filter.Eq(m => m.FoundReportId, match.FoundReportId));

        var existing = await _matches.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            await _matches.InsertOneAsync(match, cancellationToken: cancellationToken);
            return;
        }

        // Preserve Confirmed/Dismissed choices made by the user or staff
        if (existing.Status == MatchStatus.Suggested)
        {
            existing.Score = match.Score;
            existing.Explanation = match.Explanation;
            existing.UpdatedAt = DateTime.UtcNow;
            await _matches.ReplaceOneAsync(m => m.Id == existing.Id, existing, cancellationToken: cancellationToken);
        }
    }

    public async Task<ItemMatch?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken)
    {
        return await _matches.Find(m => m.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemMatch>> GetByReporterAsync(
        ObjectId lostReporterId,
        CancellationToken cancellationToken)
    {
        return await _matches
            .Find(m => m.LostReporterId == lostReporterId && m.Status != MatchStatus.Dismissed)
            .SortByDescending(m => m.Score)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItemMatch>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _matches
            .Find(Builders<ItemMatch>.Filter.Empty)
            .SortByDescending(m => m.Score)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(ItemMatch match, CancellationToken cancellationToken)
    {
        await _matches.ReplaceOneAsync(m => m.Id == match.Id, match, cancellationToken: cancellationToken);
    }
}
