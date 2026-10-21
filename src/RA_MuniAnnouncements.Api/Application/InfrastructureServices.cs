using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RA_MuniAnnouncements.Api.DataAccess;
using RA_MuniAnnouncements.Api.ExternalServices;

namespace RA_MuniAnnouncements.Api.Application;

/// <summary>
/// Everything outside the process: MongoDB (<c>ServiceCatalog</c> + <c>Users</c>) and the Forms System.
/// </summary>
public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoDb(configuration);
        services.AddFormsSystem();
        return services;
    }

    /// <summary>
    /// The <c>ServiceCatalog</c> and <c>Users</c> collections share one client.
    /// MongoClient is thread-safe and pools connections → one singleton for the whole process.
    /// </summary>
    private static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString) && !string.IsNullOrWhiteSpace(o.DatabaseName),
                "MongoDb:ConnectionString and DatabaseName are required.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5); // fail fast → 503, not a hung request
            return new MongoClient(settings);
        });

        services.AddSingleton<IServiceCatalogRepository, MongoServiceCatalogRepository>();
        services.AddSingleton<IUserRepository, MongoUserRepository>();
        return services;
    }

    /// <summary>
    /// The Forms System is still a mock — its request-by-ID API is still being built (exam Part B).
    /// Swapping to the real provider later = AddHttpClient&lt;IFormsSystemClient, FormsSystemHttpClient&gt; here;
    /// the service and endpoints don't change.
    /// </summary>
    private static IServiceCollection AddFormsSystem(this IServiceCollection services)
    {
        // Immutable in-memory data, no per-request state → singleton is safe.
        services.AddSingleton<IFormsSystemClient, MockFormsSystemClient>();
        return services;
    }
}
