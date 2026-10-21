namespace RA_MuniAnnouncements.Api.Business;

/// <summary>Configuration section "MyRequests".</summary>
public sealed class MyRequestsOptions
{
    public const string SectionName = "MyRequests";

    /// <summary>
    /// Time zone used ONLY to turn UTC instants into the displayed calendar date (dd/MM/yyyy).
    /// Decision (grooming): residents see Israel dates. Business rules (SLA) always use UTC instants.
    /// IANA id works on Linux and on Windows (.NET 6+ with ICU).
    /// </summary>
    public string DisplayTimeZoneId { get; set; } = "Asia/Jerusalem";
}
