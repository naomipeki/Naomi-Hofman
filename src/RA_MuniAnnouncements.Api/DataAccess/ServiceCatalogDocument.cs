using MongoDB.Bson.Serialization.Attributes;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.DataAccess;

/// <summary>
/// Storage shape of one <c>ServiceCatalog</c> document — exactly the exam JSON (p.5).
/// Kept separate from <see cref="ServiceCatalogEntry"/>: Mongo needs mutable, concrete collections,
/// while the business code gets read-only, ordinal lookups. <see cref="ToEntry"/> is the only bridge.
/// No _id member: Mongo generates it on insert and keeps it on replace (upsert by formCode).
/// </summary>
[BsonIgnoreExtraElements]
public sealed class ServiceCatalogDocument
{
    [BsonElement("formCode")]
    public string FormCode { get; set; } = "";

    [BsonElement("serviceName")]
    public string ServiceName { get; set; } = "";

    /// <summary>authorityId → settings. Stored as a sub-document whose field names are the authority ids.</summary>
    [BsonElement("authorities")]
    public Dictionary<string, AuthorityDocument> Authorities { get; set; } = [];

    public ServiceCatalogEntry ToEntry() => new()
    {
        FormCode = FormCode,
        ServiceName = ServiceName,
        Authorities = (Authorities ?? []).ToDictionary(a => a.Key, a => a.Value.ToDefinition(), StringComparer.Ordinal),
    };
}

[BsonIgnoreExtraElements]
public sealed class AuthorityDocument
{
    [BsonElement("authorityName")]
    public string AuthorityName { get; set; } = "";

    [BsonElement("statusMap")]
    public Dictionary<string, string> StatusMap { get; set; } = [];

    [BsonElement("actionRequiredStatuses")]
    public List<string> ActionRequiredStatuses { get; set; } = [];

    [BsonElement("finalStatuses")]
    public List<string> FinalStatuses { get; set; } = [];

    [BsonElement("slaDays")]
    public int SlaDays { get; set; }

    // A field stored as null in Mongo arrives as null → treat as empty, never crash the whole response.
    public AuthorityDefinition ToDefinition() => new()
    {
        AuthorityName = AuthorityName,
        StatusMap = new Dictionary<string, string>(StatusMap ?? [], StringComparer.Ordinal),
        ActionRequiredStatuses = new HashSet<string>(ActionRequiredStatuses ?? [], StringComparer.Ordinal),
        FinalStatuses = new HashSet<string>(FinalStatuses ?? [], StringComparer.Ordinal),
        SlaDays = SlaDays,
    };
}
