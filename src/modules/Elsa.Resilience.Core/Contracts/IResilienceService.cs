using Elsa.Workflows;

namespace Elsa.Resilience;

public interface IResilienceService
{
    Task<IEnumerable<IResilienceStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteAsync<T>(IResilientActivity activity, ActivityExecutionContext context, Func<Task<T>> action, CancellationToken cancellationToken = default);
}