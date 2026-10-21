using Microsoft.Extensions.DependencyInjection.Extensions;
using RA_MuniAnnouncements.Api.Business;

namespace RA_MuniAnnouncements.Api.Application;

/// <summary>Business layer: the services the endpoints call, their options, and the clock.</summary>
public static class BusinessServices
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MyRequestsOptions>()
            .Bind(configuration.GetSection(MyRequestsOptions.SectionName))
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.DisplayTimeZoneId, out _), "Unknown DisplayTimeZoneId")
            .ValidateOnStart();

        services.AddScoped<IMyRequestsService, MyRequestsService>();
        services.AddScoped<LoginService>();

        // Clock as a dependency: production uses the system clock; tests replace it with a fixed one.
        // TryAdd → if a test host registered a fake TimeProvider first, we don't override it.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
