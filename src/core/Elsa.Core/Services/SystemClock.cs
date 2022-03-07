using Elsa.Contracts;

namespace Elsa.Services;

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}