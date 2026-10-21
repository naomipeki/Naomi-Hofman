using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.ExternalServices;

/// <summary>
/// In-memory stand-in for the external Forms System. Holds requests of TWO fake residents
/// and filters by applicantId, so we can show that one resident never receives another's data.
///
/// Expected outcome per row (applied in Stage 2) is documented next to it.
/// Timestamps are fixed UTC instants; "overdue" depends on the clock — Stage 3 tests pin it with a fake TimeProvider.
/// </summary>
public sealed class MockFormsSystemClient : IFormsSystemClient
{
    /// <summary>Fake ID from the exam sample (p.4). Test data only.</summary>
    public const string DemoApplicantId = "012345678";

    /// <summary>A second fake resident — used to prove filtering.</summary>
    public const string OtherApplicantId = "000000018";

    private static readonly IReadOnlyList<RawFormRequest> AllRequests =
    [
        // 1. Exam sample, unchanged. Status 3 → "נדרשים מסמכים", actionRequired = true.
        //    isOverdue is clock-dependent: false until 2026-10-02T10:15Z (+30 days), true after (still open).
        Req("F-88213", "ARNONA_DISCOUNT", "6500", DemoApplicantId, "3", "2026-09-02T10:15:00Z", "2026-09-20T08:00:00Z"),

        // 2. Open ("בטיפול") and long past the 30-day SLA → overdue.
        Req("F-90001", "ARNONA_DISCOUNT", "6500", DemoApplicantId, "2", "2026-07-01T07:00:00Z", "2026-07-15T12:00:00Z"),

        // 3. Final ("אושרה"), submitted long ago → NOT overdue (final statuses are never overdue).
        Req("F-90002", "ARNONA_DISCOUNT", "6500", DemoApplicantId, "4", "2026-03-01T09:30:00Z", "2026-03-20T09:30:00Z"),

        // 4. Unknown status code "9" for authority 6500 → display "בטיפול" + warning log.
        Req("F-90003", "ARNONA_DISCOUNT", "6500", DemoApplicantId, "9", "2026-10-10T08:00:00Z", "2026-10-12T08:00:00Z"),

        // 5. Form code missing from the catalog → omitted + warning log (exam 4.5).
        Req("F-90004", "BUILDING_PERMIT", "6500", DemoApplicantId, "1", "2026-09-15T08:00:00Z", "2026-09-16T08:00:00Z"),

        // 6. Known form, but authority 8600 is not defined under it → NOT specified by the exam.
        //    Working assumption (to raise in grooming): omit + warning log, like a missing form.
        Req("F-90005", "PARKING_PERMIT", "8600", DemoApplicantId, "1", "2026-09-25T08:00:00Z", "2026-09-26T08:00:00Z"),

        // 7. SAME form as #1, different authority (7400): code "30" is action-required THERE (code "3" doesn't exist there).
        //    Updated more recently than #1 → should sort before it among action-required items.
        Req("F-90006", "ARNONA_DISCOUNT", "7400", DemoApplicantId, "30", "2026-09-28T06:00:00Z", "2026-10-05T11:00:00Z"),

        // 8. Pending, no action, most recent lastUpdate of the non-action group → first among non-action items.
        Req("F-90007", "PARKING_PERMIT", "6500", DemoApplicantId, "2", "2026-10-14T10:00:00Z", "2026-10-18T15:30:00Z"),

        // 9. SLA boundary case: 14-day SLA → boundary instant is exactly 2026-10-15T09:00:00Z.
        //    At that instant elapsed == SLA → NOT overdue (rule is strictly greater). One tick later → overdue.
        Req("F-90008", "PARKING_PERMIT", "6500", DemoApplicantId, "1", "2026-10-01T09:00:00Z", "2026-10-01T09:00:00Z"),

        // 10. Belongs to ANOTHER resident → must never appear for DemoApplicantId.
        Req("F-99999", "ARNONA_DISCOUNT", "6500", OtherApplicantId, "1", "2026-10-01T08:00:00Z", "2026-10-02T08:00:00Z"),
    ];

    public Task<IReadOnlyList<RawFormRequest>> GetRequestsByApplicantIdAsync(
        string applicantId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicantId);
        cancellationToken.ThrowIfCancellationRequested();

        // Ordinal comparison: an ID is an identifier, not language text. The ID is never logged here.
        IReadOnlyList<RawFormRequest> result = AllRequests
            .Where(r => string.Equals(r.ApplicantId, applicantId, StringComparison.Ordinal))
            .ToList();

        return Task.FromResult(result);
    }

    private static RawFormRequest Req(
        string requestId, string formCode, string authorityId, string applicantId,
        string statusCode, string submittedAtUtc, string lastUpdateUtc) => new()
        {
            RequestId = requestId,
            FormCode = formCode,
            AuthorityId = authorityId,
            ApplicantId = applicantId,
            StatusCode = statusCode,
            SubmittedAt = DateTimeOffset.Parse(submittedAtUtc, System.Globalization.CultureInfo.InvariantCulture),
            LastUpdate = DateTimeOffset.Parse(lastUpdateUtc, System.Globalization.CultureInfo.InvariantCulture),
        };
}
