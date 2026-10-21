using System.Text.Json.Serialization;

namespace RA_MuniAnnouncements.Api.Models;

/// <summary>
/// Per-authority settings nested inside a <see cref="ServiceCatalogEntry"/> (exam p.5).
/// The same form can have DIFFERENT status names, action/final sets and SLA per authority.
/// All code lookups are case-sensitive ordinal (codes are identifiers, not text).
/// </summary>
public sealed record AuthorityDefinition
{
    [JsonPropertyName("authorityName")]
    public required string AuthorityName { get; init; }

    /// <summary>statusCode → status display name, as defined by this authority.</summary>
    [JsonPropertyName("statusMap")]
    public required IReadOnlyDictionary<string, string> StatusMap { get; init; }

    /// <summary>Status codes that require an action from the resident. Backed by HashSet (ordinal) → average O(1) Contains.</summary>
    [JsonPropertyName("actionRequiredStatuses")]
    public required IReadOnlySet<string> ActionRequiredStatuses { get; init; }

    /// <summary>Status codes that close the request. A final request is never overdue.</summary>
    [JsonPropertyName("finalStatuses")]
    public required IReadOnlySet<string> FinalStatuses { get; init; }

    /// <summary>Handling-time standard in days, counted from submittedAt.</summary>
    [JsonPropertyName("slaDays")]
    public required int SlaDays { get; init; }
}
