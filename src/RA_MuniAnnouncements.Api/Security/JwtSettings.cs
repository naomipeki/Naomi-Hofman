namespace RA_MuniAnnouncements.Api.Security;

/// <summary>
/// Configuration section "Authentication:Jwt".
/// Shared settings for login token issuance and bearer validation.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Authentication:Jwt";

    public required string Issuer { get; set; }
    public required string Audience { get; set; }

    public string SigningKey { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 60;
}
