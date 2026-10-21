using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Security;

public sealed class JwtTokenIssuer(JwtSettings settings, TimeProvider clock)
{
    public LoginResponse Issue(User user)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(settings.ExpirationMinutes);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer, Audience = settings.Audience,
            IssuedAt = now.UtcDateTime, NotBefore = now.UtcDateTime, Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ResidentClaims.IdNumber, user.IdNumber),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256),
        });
        return new LoginResponse(token, expires);
    }
}
