using Elsa.Services;

namespace Elsa.Implementations;

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}