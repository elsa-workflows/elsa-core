namespace Elsa.Resilience;

public interface IResilienceService
{
    string? GetStrategyId(IResilientActivity resilientActivity);
    Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default);
    Task<IResilienceStrategy?> GetStrategyByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<T> ExecuteAsync<T>(IResilientActivity activity, Func<Task<T>> action, CancellationToken cancellationToken = default);
}