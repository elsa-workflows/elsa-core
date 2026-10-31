using Elsa.Common;

namespace Elsa.Identity.UnitTests;

/// <summary>
/// A clock that starts at the real time, which token validation compares against, and can be moved.
/// </summary>
internal sealed class MutableSystemClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
