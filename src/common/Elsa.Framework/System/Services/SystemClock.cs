namespace Elsa.Framework.System.Services;

/// <inheritdoc />
public class SystemClock : ISystemClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}