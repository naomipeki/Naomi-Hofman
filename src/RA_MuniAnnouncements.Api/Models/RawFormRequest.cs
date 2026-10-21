using System.Text.Json.Serialization;

namespace RA_MuniAnnouncements.Api.Models;

/// <summary>
/// One request record exactly as the external Forms System returns it (exam p.4).
/// Contains PII (<see cref="ApplicantId"/>) — this type must NEVER be returned to the client
/// or written to logs. Only <see cref="MyRequestDto"/> leaves the service.
/// </summary>
public sealed record RawFormRequest
{
    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    [JsonPropertyName("formCode")]
    public required string FormCode { get; init; }

    [JsonPropertyName("authorityId")]
    public required string AuthorityId { get; init; }

    /// <summary>Resident ID number (ת"ז). PII — internal use only.</summary>
    [JsonPropertyName("applicantId")]
    public required string ApplicantId { get; init; }

    [JsonPropertyName("statusCode")]
    public required string StatusCode { get; init; }

    /// <summary>UTC instant (ISO 8601 in the source JSON). Kept as an instant; formatting happens only in the output DTO.</summary>
    [JsonPropertyName("submittedAt")]
    public required DateTimeOffset SubmittedAt { get; init; }

    [JsonPropertyName("lastUpdate")]
    public required DateTimeOffset LastUpdate { get; init; }
}
