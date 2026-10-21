using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RA_MuniAnnouncements.Api.Models.Exceptions;

namespace RA_MuniAnnouncements.Api.Middleware;

/// <summary>
/// Central error → HTTP mapping. Clients get a generic ProblemDetails (RFC 9457) with a trace id;
/// exception details stay in the server log only.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The caller went away — nothing to send, not an error.
            LogRequestAborted(httpContext.TraceIdentifier);
            return true;
        }

        var (status, title) = exception switch
        {
            DependencyUnavailableException or HttpRequestException or TimeoutException =>
                (StatusCodes.Status503ServiceUnavailable, "השירות אינו זמין כרגע"),
            _ => (StatusCodes.Status500InternalServerError, "אירעה שגיאה בלתי צפויה"),
        };

        // Exception type/stack go to the log; no request data that might carry PII.
        LogUnhandled(exception, exception.GetType().Name, status, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Extensions = { ["traceId"] = httpContext.TraceIdentifier },
            },
        });
    }

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Unhandled {ExceptionType} → HTTP {StatusCode} (trace {TraceId})")]
    private partial void LogUnhandled(Exception exception, string exceptionType, int statusCode, string traceId);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Debug, Message = "Request aborted by client (trace {TraceId})")]
    private partial void LogRequestAborted(string traceId);
}
