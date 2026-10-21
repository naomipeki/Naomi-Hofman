using RA_MuniAnnouncements.Api.Endpoints;

namespace RA_MuniAnnouncements.Api.Application;

/// <summary>Single entry point for the HTTP surface: every route lives under one "/api" group.</summary>
public static class ApplicationEndpoints
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        app.MapGroup("/api")
            .MapAuthEndpoints()
            .MapMyRequestsEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();   // /openapi/v1.json, generated from the endpoints' TypedResults and metadata
        }

        return app;
    }
}
