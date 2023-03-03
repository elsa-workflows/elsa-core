using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Sinks.Contracts;

/// <summary>
/// Invokes all registered <see cref="IWorkflowSink"/>s.
/// </summary>
public interface IWorkflowSinkInvoker
{
    /// Invokes all registered <see cref="IWorkflowSink"/>s.
    Task InvokeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
}