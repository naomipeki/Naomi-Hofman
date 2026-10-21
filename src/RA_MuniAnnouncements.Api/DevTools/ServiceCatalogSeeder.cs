using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RA_MuniAnnouncements.Api.DataAccess;

namespace RA_MuniAnnouncements.Api.DevTools;

/// <summary>
/// <c>--seed-catalog [path]</c>: loads ServiceCatalog documents (exam JSON shape) into MongoDB.
/// Idempotent: upsert by formCode, so running it twice never duplicates. Also creates the unique formCode index
/// that serves the repository's <c>$in</c> query.
/// Data intentionally missing (for the demo edge cases): form BUILDING_PERMIT and authority 8600 under PARKING_PERMIT.
/// </summary>
public static class ServiceCatalogSeeder
{
    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "DataAccess", "Seed", "ServiceCatalog.json");

    public static IReadOnlyList<ServiceCatalogDocument> Load(string path) =>
        JsonSerializer.Deserialize<List<ServiceCatalogDocument>>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidDataException($"No catalog documents in {path}.");

    public static async Task SeedAsync(IServiceProvider services, string[] args)
    {
        var path = args.Length > 1 ? args[1] : DefaultPath;
        var documents = Load(path);

        var options = services.GetRequiredService<IOptions<MongoDbOptions>>().Value;
        var catalog = services.GetRequiredService<IMongoClient>()
            .GetDatabase(options.DatabaseName)
            .GetCollection<ServiceCatalogDocument>(MongoServiceCatalogRepository.CollectionName);

        await catalog.Indexes.CreateOneAsync(new CreateIndexModel<ServiceCatalogDocument>(
            Builders<ServiceCatalogDocument>.IndexKeys.Ascending(d => d.FormCode),
            new CreateIndexOptions { Unique = true }));

        foreach (var document in documents)
        {
            await catalog.ReplaceOneAsync(d => d.FormCode == document.FormCode, document, new ReplaceOptions { IsUpsert = true });
        }

        Console.WriteLine($"Seeded {documents.Count} ServiceCatalog documents from {path}.");
    }
}
