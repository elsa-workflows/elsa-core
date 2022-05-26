using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}