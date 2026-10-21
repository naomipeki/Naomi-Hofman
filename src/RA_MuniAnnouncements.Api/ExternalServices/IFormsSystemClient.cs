using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.ExternalServices;

/// <summary>
/// Client of the external Forms System (exam requirement 2).
/// ONE call returns all of the resident's requests across ALL authorities.
/// Stage 1: in-memory mock. Later: typed HttpClient (AddHttpClient) with timeout/resilience.
/// </summary>
public interface IFormsSystemClient
{
    /// <param name="applicantId">Resident ID number taken from the JWT "idNumber" claim. PII — implementations must not log it.</param>
    Task<IReadOnlyList<RawFormRequest>> GetRequestsByApplicantIdAsync(
        string applicantId,
        CancellationToken cancellationToken);
}
