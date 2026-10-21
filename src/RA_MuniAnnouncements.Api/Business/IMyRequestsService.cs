using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Business;

/// <summary>
/// Business service: fetch + normalize + sort the resident's requests (exam requirement 4).
/// Contract only in Stage 1 — implemented in Stage 2.
/// </summary>
public interface IMyRequestsService
{
    /// <param name="applicantId">Resident ID number from the validated "idNumber" claim (never from the URL).</param>
    Task<MyRequestsResponse> GetMyRequestsAsync(
        string applicantId,
        CancellationToken cancellationToken);
}
