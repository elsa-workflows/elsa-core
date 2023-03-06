using Elsa.Workflows.Sinks.Models;

namespace Elsa.Workflows.Sinks.Contracts;

/// <summary>
/// Represents a receiver of workflow state information.
/// A workflow sink can extract application-specific information and e.g. store it in a custom database. 
/// </summary>
public interface IWorkflowSink
{
    /// <summary>
    /// Implementors receive state of a workflow that just executed. 
    /// </summary>
    Task HandleWorkflowAsync(WorkflowSinkContext context, CancellationToken cancellationToken = default);
}