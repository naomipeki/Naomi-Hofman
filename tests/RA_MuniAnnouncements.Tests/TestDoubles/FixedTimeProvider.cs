namespace RA_MuniAnnouncements.Tests.TestDoubles;

/// <summary>A clock frozen at a chosen instant, so "overdue" results never depend on when tests run.</summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
