using System.Text.Json;
using Microsoft.Extensions.Logging;
using RA_MuniAnnouncements.Api.Business;
using RA_MuniAnnouncements.Api.Models;
using RA_MuniAnnouncements.Tests.TestDoubles;
using static RA_MuniAnnouncements.Tests.TestData;

namespace RA_MuniAnnouncements.Tests;

/// <summary>Unit tests of the normalization logic (exam p.5: at least 4, incl. unknown status and SLA).</summary>
public sealed class MyRequestsServiceTests
{
    // Snapshot time of the exam example: after lastUpdate (20/09), before the 30-day SLA ends (02/10 10:15Z).
    private static readonly DateTimeOffset ExamSnapshotTime = Utc("2026-09-21T09:00:00Z");

    private readonly ListLogger<MyRequestsService> _logger = new();

    private MyRequestsService CreateService(
        StubFormsClient forms, SpyCatalogRepository catalog, DateTimeOffset? now = null) =>
        new(forms, catalog, new FixedTimeProvider(now ?? ExamSnapshotTime),
            Microsoft.Extensions.Options.Options.Create(new MyRequestsOptions()), _logger);

    private async Task<IReadOnlyList<MyRequestDto>> RunAsync(DateTimeOffset now, params RawFormRequest[] requests)
    {
        var service = CreateService(new StubFormsClient(requests), new SpyCatalogRepository(ArnonaCatalog()), now);
        var response = await service.GetMyRequestsAsync(ApplicantId, CancellationToken.None);
        return response.Requests;
    }

    [Fact]
    public async Task ExamSample_IsNormalizedExactlyAsInTheSpec()
    {
        var result = await RunAsync(ExamSnapshotTime,
            Request("F-88213", statusCode: "3", submittedAt: "2026-09-02T10:15:00Z", lastUpdate: "2026-09-20T08:00:00Z"));

        var dto = Assert.Single(result);
        Assert.Equal("F-88213", dto.RequestId);
        Assert.Equal("הנחה בארנונה", dto.ServiceName);
        Assert.Equal("עיריית חדרה", dto.AuthorityName);
        Assert.Equal("נדרשים מסמכים", dto.Status);
        Assert.True(dto.ActionRequired);
        Assert.False(dto.IsOverdue);
        Assert.Equal("02/09/2026", dto.SubmittedAt);
        Assert.Equal("20/09/2026", dto.LastUpdate);
    }

    [Fact]
    public async Task UnknownStatus_IsShownAsBetipul_AndLogsWarning()
    {
        var result = await RunAsync(ExamSnapshotTime, Request("F-1", statusCode: "99"));

        var dto = Assert.Single(result);
        Assert.Equal(MyRequestsService.UnknownStatusDisplayName, dto.Status);
        Assert.False(dto.ActionRequired); // unknown code is not in the action set — decided by code, not by label

        var warning = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("F-1", warning.Message);
        Assert.Contains("99", warning.Message);
        Assert.DoesNotContain(ApplicantId, warning.Message); // no PII in logs
    }

    [Fact]
    public async Task OpenRequest_PastSla_IsOverdue()
    {
        // Submitted 02/09 10:15Z, SLA 30 days → overdue after 02/10 10:15Z.
        var result = await RunAsync(Utc("2026-10-20T00:00:00Z"), Request("F-1", statusCode: "2"));

        Assert.True(Assert.Single(result).IsOverdue);
    }

    [Fact]
    public async Task FinalStatus_IsNeverOverdue_EvenWhenVeryOld()
    {
        var result = await RunAsync(Utc("2027-12-31T00:00:00Z"),
            Request("F-approved", statusCode: "4"),
            Request("F-rejected", statusCode: "5"));

        Assert.All(result, dto => Assert.False(dto.IsOverdue));
    }

    [Theory]
    [InlineData("2026-10-02T10:15:00Z", false)]  // elapsed == 30 days exactly → NOT overdue (rule is strictly greater)
    [InlineData("2026-10-02T10:15:01Z", true)]   // one second later → overdue
    [InlineData("2026-10-02T10:14:59Z", false)]  // one second before
    public async Task SlaBoundary_IsStrictlyGreaterThan(string nowUtc, bool expectedOverdue)
    {
        var result = await RunAsync(Utc(nowUtc), Request("F-1", statusCode: "1", submittedAt: "2026-09-02T10:15:00Z"));

        Assert.Equal(expectedOverdue, Assert.Single(result).IsOverdue);
    }

    [Fact]
    public async Task FormMissingFromCatalog_IsOmitted_AndLogged()
    {
        var result = await RunAsync(ExamSnapshotTime,
            Request("F-known"),
            Request("F-unknown-form", formCode: "BUILDING_PERMIT"));

        Assert.Equal(new[] { "F-known" }, result.Select(r => r.RequestId).ToArray());
        var warning = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("BUILDING_PERMIT", warning.Message);
    }

    [Fact]
    public async Task AuthorityMissingUnderKnownForm_IsOmitted_AndLogged()
    {
        // Not specified by the exam — documented assumption: same handling as a missing form.
        var result = await RunAsync(ExamSnapshotTime, Request("F-ramat-gan", authorityId: "8600"));

        Assert.Empty(result);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("8600"));
    }

    [Fact]
    public async Task SameForm_DifferentAuthorities_UseTheirOwnRules()
    {
        // "30" means "waiting for documents" (action) in Netanya; in Hadera it is not even a known code.
        var now = Utc("2026-10-10T10:15:00Z"); // 38 days after submission: > Hadera SLA (30), < Netanya SLA (45)
        var result = await RunAsync(now,
            Request("F-netanya", statusCode: "30", authorityId: Netanya),
            Request("F-hadera", statusCode: "2", authorityId: Hadera));

        var netanya = Assert.Single(result, r => r.RequestId == "F-netanya");
        Assert.Equal("עיריית נתניה", netanya.AuthorityName);
        Assert.Equal("ממתין להשלמת מסמכים", netanya.Status);
        Assert.True(netanya.ActionRequired);
        Assert.False(netanya.IsOverdue);

        var hadera = Assert.Single(result, r => r.RequestId == "F-hadera");
        Assert.Equal("בטיפול", hadera.Status);
        Assert.False(hadera.ActionRequired);
        Assert.True(hadera.IsOverdue);
    }

    [Fact]
    public async Task Results_AreSorted_ActionRequiredFirst_ThenLastUpdateNewestFirst()
    {
        var result = await RunAsync(ExamSnapshotTime,
            Request("normal-old", statusCode: "2", lastUpdate: "2026-09-05T08:00:00Z"),
            Request("action-old", statusCode: "3", lastUpdate: "2026-09-03T08:00:00Z"),
            Request("normal-new", statusCode: "2", lastUpdate: "2026-09-20T08:00:00Z"),
            Request("action-new", statusCode: "3", lastUpdate: "2026-09-10T08:00:00Z"));

        Assert.Equal(new[] { "action-new", "action-old", "normal-new", "normal-old" }, result.Select(r => r.RequestId).ToArray());
    }

    [Fact]
    public async Task Catalog_IsQueriedOnce_WithDistinctFormCodes()
    {
        var forms = new StubFormsClient(
            Request("F-1"), Request("F-2"), Request("F-3"),
            Request("F-4", formCode: "PARKING_PERMIT"), Request("F-5", formCode: "PARKING_PERMIT"));
        var catalog = new SpyCatalogRepository(ArnonaCatalog());

        await CreateService(forms, catalog).GetMyRequestsAsync(ApplicantId, CancellationToken.None);

        Assert.Equal(1, forms.CallCount);
        var call = Assert.Single(catalog.Calls); // no N+1
        Assert.Equal(new[] { "ARNONA_DISCOUNT", "PARKING_PERMIT" }, call.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task NoRequests_ReturnsEmptyList_WithoutCallingCatalog()
    {
        var catalog = new SpyCatalogRepository(ArnonaCatalog());

        var response = await CreateService(new StubFormsClient(), catalog).GetMyRequestsAsync(ApplicantId, CancellationToken.None);

        Assert.Empty(response.Requests);
        Assert.Empty(catalog.Calls);
    }

    [Fact]
    public async Task SerializedResponse_HasExactlyTheContractFields_AndNoIdNumber()
    {
        var service = CreateService(new StubFormsClient(Request("F-1")), new SpyCatalogRepository(ArnonaCatalog()));
        var response = await service.GetMyRequestsAsync(ApplicantId, CancellationToken.None);

        var json = JsonSerializer.Serialize(response);

        Assert.DoesNotContain(ApplicantId, json);
        Assert.DoesNotContain("applicantId", json, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(json);
        var fields = document.RootElement.GetProperty("requests")[0].EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(
            new[] { "requestId", "serviceName", "authorityName", "status", "actionRequired", "isOverdue", "submittedAt", "lastUpdate" },
            fields.ToArray());
    }

    [Fact]
    public async Task DisplayDates_UseTheIsraeliCalendarDay()
    {
        // 22:30 UTC on 02/09 is already 03/09 01:30 in Israel (UTC+3 in September).
        var result = await RunAsync(ExamSnapshotTime, Request("F-1", submittedAt: "2026-09-02T22:30:00Z"));

        Assert.Equal("03/09/2026", Assert.Single(result).SubmittedAt);
    }
}
