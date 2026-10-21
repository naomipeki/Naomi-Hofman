using System.Globalization;
using Microsoft.Extensions.Options;
using RA_MuniAnnouncements.Api.DataAccess;
using RA_MuniAnnouncements.Api.ExternalServices;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Business;

/// <summary>
/// Fetches the resident's requests and normalizes them by the per-authority catalog settings (exam requirement 4).
///
/// Cost for R raw requests and C distinct form codes:
///   1 call to the Forms System + 1 batch call to the catalog (0 if R = 0),
///   O(R) to build the code set and normalize (average O(1) Dictionary/HashSet lookups),
///   O(R log R) to sort.  No N+1: there is no await inside the loop.
/// </summary>
public sealed partial class MyRequestsService : IMyRequestsService
{
    /// <summary>Exam 4.2: display name for a status code the authority did not define.</summary>
    public const string UnknownStatusDisplayName = "בטיפול";

    /// <summary>Exam sample format. "/" is a literal here only because we format with InvariantCulture.</summary>
    public const string DisplayDateFormat = "dd/MM/yyyy";

    private readonly IFormsSystemClient _formsClient;
    private readonly IServiceCatalogRepository _catalogRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MyRequestsService> _logger;
    private readonly TimeZoneInfo _displayTimeZone;

    public MyRequestsService(
        IFormsSystemClient formsClient,
        IServiceCatalogRepository catalogRepository,
        TimeProvider timeProvider,
        IOptions<MyRequestsOptions> options,
        ILogger<MyRequestsService> logger)
    {
        _formsClient = formsClient;
        _catalogRepository = catalogRepository;
        _timeProvider = timeProvider;
        _logger = logger;
        _displayTimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.DisplayTimeZoneId);
    }

    public async Task<MyRequestsResponse> GetMyRequestsAsync(string applicantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicantId);

        // 1) ONE call: all of the resident's requests from all authorities.
        var rawRequests = await _formsClient.GetRequestsByApplicantIdAsync(applicantId, cancellationToken);
        if (rawRequests.Count == 0)
        {
            return new MyRequestsResponse { Requests = [] }; // nothing to look up → no catalog call
        }

        // 2) Distinct form codes → ONE batch catalog read.
        var formCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in rawRequests)
        {
            formCodes.Add(raw.FormCode);
        }

        var catalog = await _catalogRepository.GetByFormCodesAsync(formCodes, cancellationToken);

        // 3) Normalize in memory. One clock read → every request is judged against the same "now".
        var utcNow = _timeProvider.GetUtcNow();
        var normalized = new List<(MyRequestDto Dto, DateTimeOffset LastUpdate)>(rawRequests.Count);

        foreach (var raw in rawRequests)
        {
            if (!catalog.TryGetValue(raw.FormCode, out var catalogEntry))
            {
                LogFormNotInCatalog(raw.RequestId, raw.FormCode); // exam 4.5: omit + log
                continue;
            }

            if (!catalogEntry.Authorities.TryGetValue(raw.AuthorityId, out var authority))
            {
                // NOT specified by the exam. Working assumption (grooming question): treat like a missing form.
                LogAuthorityNotInCatalog(raw.RequestId, raw.FormCode, raw.AuthorityId);
                continue;
            }

            normalized.Add((Normalize(raw, catalogEntry, authority, utcNow), raw.LastUpdate));
        }

        // 4) Exam 4.6: action-required first, then lastUpdate newest → oldest.
        //    Sort by the real instant, never by the formatted "dd/MM/yyyy" string.
        //    RequestId is a final tie-breaker so equal items always come out in the same order.
        var ordered = normalized
            .OrderByDescending(item => item.Dto.ActionRequired)
            .ThenByDescending(item => item.LastUpdate)
            .ThenBy(item => item.Dto.RequestId, StringComparer.Ordinal)
            .Select(item => item.Dto)
            .ToList();

        return new MyRequestsResponse { Requests = ordered };
    }

    private MyRequestDto Normalize(
        RawFormRequest raw,
        ServiceCatalogEntry catalogEntry,
        AuthorityDefinition authority,
        DateTimeOffset utcNow)
    {
        // Exam 4.2: translate by THIS authority's map; unknown → "בטיפול" + log.
        if (!authority.StatusMap.TryGetValue(raw.StatusCode, out var statusName))
        {
            statusName = UnknownStatusDisplayName;
            LogUnknownStatus(raw.RequestId, raw.FormCode, raw.AuthorityId, raw.StatusCode);
        }

        // Rules use the raw CODE sets, never the translated label.
        var actionRequired = authority.ActionRequiredStatuses.Contains(raw.StatusCode); // exam 4.3

        // Exam 4.4: open (not final) AND elapsed STRICTLY greater than the SLA. Exactly SLA → not overdue.
        var isFinal = authority.FinalStatuses.Contains(raw.StatusCode);
        var isOverdue = !isFinal && (utcNow - raw.SubmittedAt) > TimeSpan.FromDays(authority.SlaDays);

        // Whitelist mapping: every field is set explicitly. ApplicantId has no target property (exam 4.7).
        return new MyRequestDto
        {
            RequestId = raw.RequestId,
            ServiceName = catalogEntry.ServiceName,
            AuthorityName = authority.AuthorityName,
            Status = statusName,
            ActionRequired = actionRequired,
            IsOverdue = isOverdue,
            SubmittedAt = FormatDate(raw.SubmittedAt),
            LastUpdate = FormatDate(raw.LastUpdate),
        };
    }

    private string FormatDate(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, _displayTimeZone).ToString(DisplayDateFormat, CultureInfo.InvariantCulture);

    // Structured, privacy-safe logs: request/form/authority/status codes only — never the ID number.
    // Source-generated (LoggerMessage): no boxing, no string building when the level is disabled.
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "Request {RequestId} skipped: form code {FormCode} not found in ServiceCatalog")]
    private partial void LogFormNotInCatalog(string requestId, string formCode);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Request {RequestId} skipped: authority {AuthorityId} not defined for form {FormCode}")]
    private partial void LogAuthorityNotInCatalog(string requestId, string formCode, string authorityId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning,
        Message = "Request {RequestId}: unknown status code {StatusCode} for form {FormCode} / authority {AuthorityId}; shown as fallback")]
    private partial void LogUnknownStatus(string requestId, string formCode, string authorityId, string statusCode);
}
