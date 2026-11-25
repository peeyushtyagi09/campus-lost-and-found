using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken);

    Task<User?> GetByEmailHashAsync(string emailHash, CancellationToken cancellationToken);

    Task CreateAsync(User user, CancellationToken cancellationToken);

    Task UpdateLoginStateAsync(
        ObjectId id,
        int failedLoginCount,
        DateTime? lockoutEnd,
        string? upgradedPasswordHash,
        CancellationToken cancellationToken);
}
