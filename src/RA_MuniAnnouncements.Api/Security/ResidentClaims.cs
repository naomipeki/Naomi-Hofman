namespace RA_MuniAnnouncements.Api.Security;

/// <summary>Names shared by authentication, authorization and the endpoints.</summary>
public static class ResidentClaims
{
    /// <summary>Claim carrying the resident's ID number in the Personal Area token (exam requirement 1).</summary>
    public const string IdNumber = "idNumber";

    /// <summary>Policy: authenticated AND carries a non-empty idNumber claim.</summary>
    public const string ResidentPolicy = "Resident";
}
