namespace RA_MuniAnnouncements.Api.DataAccess;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "";
}
