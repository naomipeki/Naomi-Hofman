using System.Text.Json.Serialization;

namespace RA_MuniAnnouncements.Api.Models;

/// <summary>Response envelope of GET /api/my-requests: <c>{ "requests": [ ... ] }</c> (exam p.5).</summary>
public sealed record MyRequestsResponse
{
    [JsonPropertyName("requests")]
    public required IReadOnlyList<MyRequestDto> Requests { get; init; }
}
