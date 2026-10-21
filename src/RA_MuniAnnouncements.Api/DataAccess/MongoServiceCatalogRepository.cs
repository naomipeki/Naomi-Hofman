using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RA_MuniAnnouncements.Api.Models.Exceptions;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.DataAccess;

/// <summary>
/// MongoDB <c>ServiceCatalog</c> collection (exam requirement 3).
/// ONE query per call: <c>{ formCode: { $in: [...] } }</c>, served by the unique index on formCode
/// (created by <c>--seed-catalog</c>).
/// </summary>
public sealed class MongoServiceCatalogRepository : IServiceCatalogRepository
{
    public const string CollectionName = "ServiceCatalog";

    private readonly IMongoCollection<ServiceCatalogDocument> _catalog;

    public MongoServiceCatalogRepository(IMongoClient client, IOptions<MongoDbOptions> options)
        => _catalog = client.GetDatabase(options.Value.DatabaseName).GetCollection<ServiceCatalogDocument>(CollectionName);

    public async Task<IReadOnlyDictionary<string, ServiceCatalogEntry>> GetByFormCodesAsync(
        IReadOnlyCollection<string> formCodes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formCodes);
        var hits = new Dictionary<string, ServiceCatalogEntry>(formCodes.Count, StringComparer.Ordinal);
        if (formCodes.Count == 0)
        {
            return hits;
        }

        List<ServiceCatalogDocument> documents;
        try
        {
            documents = await _catalog
                .Find(Builders<ServiceCatalogDocument>.Filter.In(d => d.FormCode, formCodes))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            throw new DependencyUnavailableException(CollectionName, ex);
        }

        foreach (var document in documents)
        {
            hits[document.FormCode] = document.ToEntry(); // indexer → tolerant of duplicates if the index is missing
        }

        return hits;
    }
}
