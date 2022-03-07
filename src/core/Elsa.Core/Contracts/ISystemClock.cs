namespace Elsa.Contracts;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}