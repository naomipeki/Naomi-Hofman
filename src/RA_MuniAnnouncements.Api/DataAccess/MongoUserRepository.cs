using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RA_MuniAnnouncements.Api.Models.Exceptions;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.DataAccess;

public sealed class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> users;
    public MongoUserRepository(IMongoClient client, IOptions<MongoDbOptions> options)
        => users = client.GetDatabase(options.Value.DatabaseName).GetCollection<User>("Users");

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        try
        {
            return await users.Find(u => u.NormalizedUsername == User.NormalizeUsername(username))
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            throw new DependencyUnavailableException("Users", ex);
        }
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        // Explicit provisioning only: no seeded accounts or public registration endpoint.
        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.NormalizedUsername),
            new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        await users.InsertOneAsync(user, cancellationToken: cancellationToken);
    }
}
