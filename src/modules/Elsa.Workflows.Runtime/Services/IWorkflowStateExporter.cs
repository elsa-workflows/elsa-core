using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A workflow state exporter takes as input some workflow state and persists it in a certain way.
/// Examples are storing the workflow state in a database or indexing using Elastic Search.
/// </summary>
public interface IWorkflowStateExporter
{
    ValueTask ExportAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken);
}