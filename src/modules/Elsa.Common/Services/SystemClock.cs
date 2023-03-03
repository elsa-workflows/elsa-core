using Elsa.Common.Contracts;

namespace Elsa.Common.Services;

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}