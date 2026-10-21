using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.DataAccess;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task CreateAsync(User user, CancellationToken cancellationToken);
}
