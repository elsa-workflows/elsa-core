using Elsa.Workflows.Core.Activities;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// A workflow activation validator controls whether new instances are allowed to be created given certain conditions.
/// </summary>
public interface IWorkflowActivationStrategy
{
    /// <summary>
    /// Returns true if a new workflow instance should be created, false otherwise.
    /// </summary>
    ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context);
}

/// <summary>
/// Provides context about the request for allowing the creation of a new workflow instance. 
/// </summary>
public record WorkflowInstantiationStrategyContext(Workflow Workflow, string? CorrelationId, CancellationToken CancellationToken);