using Elsa.Workflows;

namespace Elsa.Resilience;

public interface IResilientActivityInvoker
{
    Task<T> InvokeAsync<T>(IResilientActivity activity, ActivityExecutionContext context, Func<Task<T>> action, CancellationToken cancellationToken = default);
}