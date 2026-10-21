using RA_MuniAnnouncements.Api.DataAccess;
using RA_MuniAnnouncements.Api.DevTools;

namespace RA_MuniAnnouncements.Tests;

/// <summary>The seed file and the Mongo document → business model mapping.</summary>
public sealed class ServiceCatalogDocumentTests
{
    [Fact]
    public void SeedFile_ContainsTheExamDocument()
    {
        var entry = Assert.Single(ServiceCatalogSeeder.Load(ServiceCatalogSeeder.DefaultPath), d => d.FormCode == "ARNONA_DISCOUNT").ToEntry();

        Assert.Equal("הנחה בארנונה", entry.ServiceName);
        var hadera = entry.Authorities["6500"];
        Assert.Equal("עיריית חדרה", hadera.AuthorityName);
        Assert.Equal("נדרשים מסמכים", hadera.StatusMap["3"]);
        Assert.Equal(["3"], hadera.ActionRequiredStatuses);
        Assert.Equal(["4", "5"], hadera.FinalStatuses.Order(StringComparer.Ordinal));
        Assert.Equal(30, hadera.SlaDays);
    }

    [Fact]
    public void SeedFile_LeavesOutTheDemoEdgeCases()
    {
        var documents = ServiceCatalogSeeder.Load(ServiceCatalogSeeder.DefaultPath);

        Assert.DoesNotContain(documents, d => d.FormCode == "BUILDING_PERMIT");                    // missing form
        Assert.DoesNotContain("8600", documents.Single(d => d.FormCode == "PARKING_PERMIT").Authorities.Keys); // missing authority
    }

    [Fact]
    public void ToEntry_UsesOrdinalLookups_AndTreatsNullCollectionsAsEmpty()
    {
        var document = new ServiceCatalogDocument
        {
            FormCode = "F",
            ServiceName = "S",
            Authorities = new()
            {
                ["1"] = new AuthorityDocument
                {
                    AuthorityName = "A",
                    StatusMap = null!,
                    ActionRequiredStatuses = ["x"],
                    FinalStatuses = null!,
                    SlaDays = 5,
                },
            },
        };

        var authority = document.ToEntry().Authorities["1"];

        Assert.Empty(authority.StatusMap);
        Assert.Empty(authority.FinalStatuses);
        Assert.Contains("x", authority.ActionRequiredStatuses);
        Assert.DoesNotContain("X", authority.ActionRequiredStatuses); // codes are case-sensitive identifiers
    }
}
