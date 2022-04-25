namespace Elsa.Services;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}