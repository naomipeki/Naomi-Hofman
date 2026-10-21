using RA_MuniAnnouncements.Api.Middleware;

namespace RA_MuniAnnouncements.Api.Application;

/// <summary>
/// Composition root for DI: Program calls this one method; each layer registers itself in its own file.
/// </summary>
public static class ApplicationServices
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddInfrastructureServices(configuration);
        services.AddBusinessServices(configuration);
        services.AddResidentAuthentication(configuration, environment);

        // --- Web layer ---
        services.AddValidation();   // DataAnnotations on Minimal API parameters → 400 ValidationProblem
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
