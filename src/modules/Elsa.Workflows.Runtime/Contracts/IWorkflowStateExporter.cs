using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// A workflow state exporter takes as input some workflow state and persists it in a certain way.
/// Examples are storing the workflow state in a database or indexing using Elastic Search.
/// </summary>
public interface IWorkflowStateExporter
{
    /// <summary>
    /// Exports the workflow state.
    /// </summary>
    /// <param name="workflow"></param>
    /// <param name="workflowState"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask ExportAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken);
}