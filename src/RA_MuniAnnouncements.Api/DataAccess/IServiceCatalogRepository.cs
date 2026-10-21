using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.DataAccess;

/// <summary>
/// Read access to the MongoDB <c>ServiceCatalog</c> collection (exam requirement 3).
/// Set-based by design: the caller passes ALL distinct form codes at once and the
/// implementation performs ONE logical read (Mongo: formCode $in [...]) — never one call per code (no N+1).
/// </summary>
public interface IServiceCatalogRepository
{
    /// <returns>
    /// formCode → entry, ONLY for codes that exist. Missing codes are simply absent from the result;
    /// the caller decides what to do (exam 4.5: omit + log).
    /// </returns>
    Task<IReadOnlyDictionary<string, ServiceCatalogEntry>> GetByFormCodesAsync(
        IReadOnlyCollection<string> formCodes,
        CancellationToken cancellationToken);
}
