using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Services;

public interface IWorkflowStateExporterService
{
    Task ExportWorkflowStateAsync(WorkflowDefinition definition, WorkflowState workflowState, CancellationToken cancellationToken = default);
}