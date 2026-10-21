namespace RA_MuniAnnouncements.Api.Application;

/// <summary>The HTTP request pipeline. Order matters: errors first, then rate limiting, then auth.</summary>
public static class ApplicationMiddleware
{
    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler();   // unhandled exceptions → ProblemDetails (503/500), no stack traces to clients
        app.UseStatusCodePages();    // bare 401/403/404 → ProblemDetails bodies too
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
