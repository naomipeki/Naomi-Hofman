using RA_MuniAnnouncements.Api.DevTools;

namespace RA_MuniAnnouncements.Api.Application;

/// <summary>Explicit one-off commands; nothing is seeded or provisioned at startup.</summary>
public static class CommandLineTools
{
    /// <returns><c>true</c> when a command ran and the process should exit instead of serving HTTP.</returns>
    public static async Task<bool> TryRunCommandAsync(this WebApplication app, string[] args)
    {
        switch (args.FirstOrDefault())
        {
            case "--create-user":
                await UserProvisioner.CreateAsync(app.Services, args);
                return true;
            case "--seed-catalog":
                await ServiceCatalogSeeder.SeedAsync(app.Services, args);
                return true;
            default:
                return false;
        }
    }
}
