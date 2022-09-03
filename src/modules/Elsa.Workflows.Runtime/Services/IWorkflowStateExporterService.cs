using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Runtime.Services;

public interface IWorkflowStateExporterService
{
    Task ExportWorkflowStateAsync(WorkflowDefinition definition, WorkflowState workflowState, CancellationToken cancellationToken = default);
}