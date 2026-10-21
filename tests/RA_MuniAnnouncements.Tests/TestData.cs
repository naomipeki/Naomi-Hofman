using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Tests;

/// <summary>Small builders so each test shows only the fields it cares about.</summary>
internal static class TestData
{
    public const string ApplicantId = "012345678";
    public const string Hadera = "6500";
    public const string Netanya = "7400";

    /// <summary>The exam's ServiceCatalog document (p.5) + a second authority with different rules.</summary>
    public static ServiceCatalogEntry ArnonaCatalog() => new()
    {
        FormCode = "ARNONA_DISCOUNT",
        ServiceName = "הנחה בארנונה",
        Authorities = new Dictionary<string, AuthorityDefinition>(StringComparer.Ordinal)
        {
            [Hadera] = Authority("עיריית חדרה", slaDays: 30, action: ["3"], final: ["4", "5"],
                ("1", "התקבלה"), ("2", "בטיפול"), ("3", "נדרשים מסמכים"), ("4", "אושרה"), ("5", "נדחתה")),
            [Netanya] = Authority("עיריית נתניה", slaDays: 45, action: ["30"], final: ["40"],
                ("10", "נקלטה"), ("20", "בבדיקה"), ("30", "ממתין להשלמת מסמכים"), ("40", "הטיפול הסתיים")),
        },
    };

    public static AuthorityDefinition Authority(
        string name, int slaDays, string[] action, string[] final, params (string Code, string Name)[] statuses) => new()
        {
            AuthorityName = name,
            SlaDays = slaDays,
            ActionRequiredStatuses = new HashSet<string>(action, StringComparer.Ordinal),
            FinalStatuses = new HashSet<string>(final, StringComparer.Ordinal),
            StatusMap = statuses.ToDictionary(s => s.Code, s => s.Name, StringComparer.Ordinal),
        };

    public static RawFormRequest Request(
        string requestId,
        string statusCode = "2",
        string formCode = "ARNONA_DISCOUNT",
        string authorityId = Hadera,
        string submittedAt = "2026-09-02T10:15:00Z",
        string? lastUpdate = null) => new()
        {
            RequestId = requestId,
            FormCode = formCode,
            AuthorityId = authorityId,
            ApplicantId = ApplicantId,
            StatusCode = statusCode,
            SubmittedAt = Utc(submittedAt),
            LastUpdate = Utc(lastUpdate ?? submittedAt),
        };

    public static DateTimeOffset Utc(string iso) =>
        DateTimeOffset.Parse(iso, System.Globalization.CultureInfo.InvariantCulture);
}
