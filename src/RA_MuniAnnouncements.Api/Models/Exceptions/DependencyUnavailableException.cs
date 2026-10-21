namespace RA_MuniAnnouncements.Api.Models.Exceptions;

/// <summary>
/// Thrown by a real client/repository when a dependency (Forms System, ServiceCatalog) is down or too slow.
/// Mapped to 503 so the UI shows "service unavailable + retry" — never a misleading empty list.
/// The message must not contain PII.
/// </summary>
public sealed class DependencyUnavailableException(string dependencyName, Exception? innerException = null)
    : Exception($"Dependency '{dependencyName}' is unavailable.", innerException)
{
    public string DependencyName { get; } = dependencyName;
}
