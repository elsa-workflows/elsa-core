namespace Elsa.Common.Services;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}