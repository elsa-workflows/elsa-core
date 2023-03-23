using Elsa.Workflows.Core.Abstractions;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Implement this interface or <see cref="WorkflowBase"/> when implementing workflows using code so that they become available to the system.
/// </summary>
public interface IWorkflow
{
    /// <summary>
    /// Invokes the the specified <see cref="IWorkflowBuilder"/>.
    /// </summary>
    ValueTask BuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default);
}