using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public MongoUserRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _users = context.Users;
    }

    public async Task<User?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken)
    {
        return await _users
            .Find(user => user.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailHashAsync(string emailHash, CancellationToken cancellationToken)
    {
        return await _users
            .Find(user => user.EmailHash == emailHash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            await _users.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateEmailException(exception);
        }
    }

    public async Task UpdateLoginStateAsync(
        ObjectId id,
        int failedLoginCount,
        DateTime? lockoutEnd,
        string? upgradedPasswordHash,
        CancellationToken cancellationToken)
    {
        var update = Builders<User>.Update
            .Set(user => user.FailedLoginCount, failedLoginCount)
            .Set(user => user.LockoutEnd, lockoutEnd);

        if (upgradedPasswordHash is not null)
        {
            update = update.Set(user => user.PasswordHash, upgradedPasswordHash);
        }

        await _users.UpdateOneAsync(
            user => user.Id == id,
            update,
            cancellationToken: cancellationToken);
    }
}
