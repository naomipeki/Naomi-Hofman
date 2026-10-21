using Microsoft.AspNetCore.Http.HttpResults;
using RA_MuniAnnouncements.Api.Application;
using RA_MuniAnnouncements.Api.Business;
using RA_MuniAnnouncements.Api.Models;

namespace RA_MuniAnnouncements.Api.Endpoints;

/// <summary>POST /api/auth/login — MongoDB Users + password hash → signed JWT.</summary>
public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Exchanges username and password for a resident JWT")
            .AllowAnonymous()
            .RequireRateLimiting(AuthenticationServices.LoginRateLimitPolicy)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return api;
    }

    // Body validation ([Required], [StringLength]) runs before this handler (AddValidation) → 400.
    public static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Login(
        LoginRequest request, LoginService login, HttpResponse response, CancellationToken cancellationToken)
    {
        response.Headers.CacheControl = "no-store";
        var result = await login.LoginAsync(request, cancellationToken);
        return result is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "שם המשתמש או הסיסמה שגויים")
            : TypedResults.Ok(result);
    }
}
