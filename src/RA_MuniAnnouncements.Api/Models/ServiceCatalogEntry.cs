using System.Text.Json.Serialization;

namespace RA_MuniAnnouncements.Api.Models;

/// <summary>
/// One document of the MongoDB collection <c>ServiceCatalog</c> (exam p.5).
/// Form-level data (serviceName) sits here; authority-level data sits in <see cref="Authorities"/>.
/// </summary>
public sealed record ServiceCatalogEntry
{
    [JsonPropertyName("formCode")]
    public required string FormCode { get; init; }

    [JsonPropertyName("serviceName")]
    public required string ServiceName { get; init; }

    /// <summary>authorityId → that authority's definition for this form. Ordinal keys.</summary>
    [JsonPropertyName("authorities")]
    public required IReadOnlyDictionary<string, AuthorityDefinition> Authorities { get; init; }
}
