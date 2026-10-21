using RA_MuniAnnouncements.Api.DataAccess;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Tests.TestDoubles;

/// <summary>In-memory catalog that records every call — proves the service batches (one call, distinct codes).</summary>
public sealed class SpyCatalogRepository(params ServiceCatalogEntry[] entries) : IServiceCatalogRepository
{
    private readonly Dictionary<string, ServiceCatalogEntry> _entries =
        entries.ToDictionary(e => e.FormCode, StringComparer.Ordinal);

    public List<IReadOnlyCollection<string>> Calls { get; } = [];

    public Task<IReadOnlyDictionary<string, ServiceCatalogEntry>> GetByFormCodesAsync(
        IReadOnlyCollection<string> formCodes, CancellationToken cancellationToken)
    {
        Calls.Add(formCodes.ToList()); // snapshot of what was asked
        var hits = formCodes
            .Where(_entries.ContainsKey)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(code => code, code => _entries[code], StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyDictionary<string, ServiceCatalogEntry>>(hits);
    }
}
