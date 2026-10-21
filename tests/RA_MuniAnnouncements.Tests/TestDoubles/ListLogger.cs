using Microsoft.Extensions.Logging;

namespace RA_MuniAnnouncements.Tests.TestDoubles;

/// <summary>Captures log entries so tests can assert that a warning was written (and what it contained).</summary>
public sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, eventId, formatter(state, exception)));
}
