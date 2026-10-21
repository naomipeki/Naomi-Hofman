using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RA_MuniAnnouncements.Api.Models;
using RA_MuniAnnouncements.Api.Security;

namespace RA_MuniAnnouncements.Api.Application;

public static class AuthenticationServices
{
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddResidentAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Authentication:Jwt is required.");
        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32 ||
            string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience) ||
            jwt.ExpirationMinutes is < 1 or > 1440 ||
            (!environment.IsDevelopment() && jwt.SigningKey.StartsWith("LOCAL-DEV-ONLY", StringComparison.Ordinal)))
            throw new InvalidOperationException("Configure Authentication:Jwt with a signing key of at least 32 UTF-8 bytes, issuer, audience and an expiration of 1–1440 minutes. The local development key is forbidden outside Development.");

        services.AddSingleton(jwt);
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<JwtTokenIssuer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateLifetime = true, ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.Zero,
            };
        });
        services.AddAuthorizationBuilder()
            .AddPolicy(ResidentClaims.ResidentPolicy, policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst(ResidentClaims.IdNumber)?.Value)));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
                }));
        });
        return services;
    }
}
