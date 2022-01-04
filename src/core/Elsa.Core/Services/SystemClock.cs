using Elsa.Contracts;

namespace Elsa.Services;

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}