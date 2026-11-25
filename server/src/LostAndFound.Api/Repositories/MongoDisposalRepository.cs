using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoDisposalRepository : IDisposalRepository
{
    private readonly IMongoCollection<Disposal> _disposals;

    public MongoDisposalRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _disposals = context.Disposals;
    }

    public async Task CreateAsync(Disposal disposal, CancellationToken cancellationToken)
    {
        await _disposals.InsertOneAsync(disposal, cancellationToken: cancellationToken);
    }

    public async Task<Disposal?> GetByFoundReportIdAsync(ObjectId foundReportId, CancellationToken cancellationToken)
    {
        return await _disposals.Find(d => d.FoundReportId == foundReportId).FirstOrDefaultAsync(cancellationToken);
    }
}
