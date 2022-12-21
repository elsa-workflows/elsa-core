using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// A workflow instantiation strategy controls whether new instances are allowed to be created given certain conditions.
/// </summary>
public interface IWorkflowInstantiationStrategy
{
    /// <summary>
    /// Returns true if a new workflow instance should be created, false otherwise.
    /// </summary>
    ValueTask<bool> ShouldCreateInstanceAsync(WorkflowInstantiationStrategyContext context);
}

/// <summary>
/// Provides context about the request for allowing the creation of a new workflow instance. 
/// </summary>
public record WorkflowInstantiationStrategyContext(Workflow Workflow, string? CorrelationId, CancellationToken CancellationToken);