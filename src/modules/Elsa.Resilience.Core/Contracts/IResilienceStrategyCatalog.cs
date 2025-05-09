namespace Elsa.Resilience;

public interface IResilienceStrategyCatalog
{
    Task<IEnumerable<IResilienceStrategy>> ListAsync(CancellationToken cancellationToken = default);
    Task<IResilienceStrategy?> GetAsync(string id, CancellationToken cancellationToken = default);
}