using Elsa.Common;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

internal sealed class TestSystemClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}
