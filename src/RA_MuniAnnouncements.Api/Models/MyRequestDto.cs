using System.Text.Json.Serialization;

namespace RA_MuniAnnouncements.Api.Models;

/// <summary>
/// Outward contract — the ONLY request shape that leaves the service (exam p.5).
/// Whitelist by construction: exactly 8 fields, and there is deliberately NO applicantId property,
/// so the ID number cannot be serialized to the client even by mistake.
/// Dates are pre-formatted strings "dd/MM/yyyy" (filled by the service in Stage 2).
/// </summary>
public sealed record MyRequestDto
{
    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    [JsonPropertyName("serviceName")]
    public required string ServiceName { get; init; }

    [JsonPropertyName("authorityName")]
    public required string AuthorityName { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("actionRequired")]
    public required bool ActionRequired { get; init; }

    [JsonPropertyName("isOverdue")]
    public required bool IsOverdue { get; init; }

    /// <summary>Format "dd/MM/yyyy", as in the exam sample.</summary>
    [JsonPropertyName("submittedAt")]
    public required string SubmittedAt { get; init; }

    /// <summary>Format "dd/MM/yyyy", as in the exam sample.</summary>
    [JsonPropertyName("lastUpdate")]
    public required string LastUpdate { get; init; }
}
