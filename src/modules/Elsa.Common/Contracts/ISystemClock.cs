namespace Elsa.Common.Contracts;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}