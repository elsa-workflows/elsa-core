namespace Elsa.Resilience;

public interface IResilienceStrategyCatalog
{
    Task<IEnumerable<IResilienceStrategy>> GetAllStrategiesAsync(CancellationToken cancellationToken = default);
    Task<IResilienceStrategy?> GetStrategyAsync(string id, CancellationToken cancellationToken = default);
}