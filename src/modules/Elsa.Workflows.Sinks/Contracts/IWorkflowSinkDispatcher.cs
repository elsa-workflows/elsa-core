using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Sinks.Contracts;

/// <summary>
/// Dispatches <see cref="WorkflowState"/>s to a consumer that in turn invokes any registered <see cref="IWorkflowSink"/>s. 
/// </summary>
public interface IWorkflowSinkDispatcher
{
    /// <summary>
    /// Dispatches the specified <see cref="WorkflowState"/>.
    /// </summary>
    Task DispatchAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
}