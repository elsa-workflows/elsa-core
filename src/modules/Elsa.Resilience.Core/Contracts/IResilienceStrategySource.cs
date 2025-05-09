namespace Elsa.Resilience;

public interface IResilienceStrategySource
{
    Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default);
}