namespace Elsa.Framework.System;

/// <summary>
/// Represents a system clock.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current date and time in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}