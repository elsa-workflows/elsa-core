namespace Elsa.Workflows.Core.Services;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}