namespace Elsa.Resilience;

public interface IResilienceStrategyProvider
{
    Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default);
}