using Microsoft.AspNetCore.Identity;
using RA_MuniAnnouncements.Api.DataAccess;
using RA_MuniAnnouncements.Api.Models;
using RA_MuniAnnouncements.Api.Security;

namespace RA_MuniAnnouncements.Api.Business;

public sealed class LoginService(IUserRepository users, IPasswordHasher<User> passwords, JwtTokenIssuer tokens)
{
    private static readonly User MissingUser = new();
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(MissingUser, Guid.NewGuid().ToString());

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByUsernameAsync(request.Username, cancellationToken);
        PasswordVerificationResult result;
        try
        {
            // Missing users also perform password verification to reduce timing differences.
            result = passwords.VerifyHashedPassword(user ?? MissingUser, user?.PasswordHash ?? DummyHash, request.Password);
        }
        catch (FormatException)
        {
            return null;
        }
        return user is null || result == PasswordVerificationResult.Failed || string.IsNullOrWhiteSpace(user.IdNumber)
            ? null : tokens.Issue(user);
    }
}
