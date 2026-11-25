using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

public interface IDisposalRepository
{
    Task CreateAsync(Disposal disposal, CancellationToken cancellationToken);

    Task<Disposal?> GetByFoundReportIdAsync(ObjectId foundReportId, CancellationToken cancellationToken);
}
