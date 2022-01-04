namespace Elsa.Contracts;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}