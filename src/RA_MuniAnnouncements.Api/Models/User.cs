using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace RA_MuniAnnouncements.Api.Models;

[BsonIgnoreExtraElements]
public sealed class User
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Username { get; set; } = "";
    public string NormalizedUsername { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string IdNumber { get; set; } = "";
    public static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();
}
