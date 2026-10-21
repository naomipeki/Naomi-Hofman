using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using RA_MuniAnnouncements.Api.Business;
using RA_MuniAnnouncements.Api.Models;
using RA_MuniAnnouncements.Api.Security;

namespace RA_MuniAnnouncements.Api.Endpoints;

/// <summary>
/// GET /api/my-requests — the resident's requests from all authorities, normalized.
/// Identity comes ONLY from the validated token's "idNumber" claim. The route has no id parameter,
/// and nothing from the query string is bound, so one resident cannot ask for another's data.
/// </summary>
public static class MyRequestsEndpoints
{
    public static RouteGroupBuilder MapMyRequestsEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/my-requests", Get)
            .WithName("GetMyRequests")
            .WithTags("MyRequests")
            .WithSummary("The signed-in resident's requests from all authorities")
            .RequireAuthorization(ResidentClaims.ResidentPolicy)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return api;
    }

    public static async Task<Results<Ok<MyRequestsResponse>, ForbidHttpResult>> Get(
        ClaimsPrincipal user, IMyRequestsService service, CancellationToken cancellationToken)
    {
        // The policy already guarantees the claim; this check is defense in depth.
        var idNumber = user.FindFirst(ResidentClaims.IdNumber)?.Value;
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return TypedResults.Forbid();
        }

        // cancellationToken = HttpContext.RequestAborted: if the browser leaves, downstream calls stop too.
        return TypedResults.Ok(await service.GetMyRequestsAsync(idNumber.Trim(), cancellationToken));
    }
}
