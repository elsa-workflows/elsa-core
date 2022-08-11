using Elsa.Common.Services;

namespace Elsa.Common.Implementations;

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}