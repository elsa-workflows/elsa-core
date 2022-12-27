using Elsa.Mediator.Models;
using Elsa.Mediator.Services;

namespace Elsa.Workflows.Sink.Models;

public class ExportWorkflowSinkMessage : ICommand<Unit>
{
    public ExportWorkflowSinkMessage(string workflowDefinitionId, int workflowDefinitionVersion, string workflowStateId)
    {
        WorkflowDefinitionId = workflowDefinitionId;
        WorkflowDefinitionVersion = workflowDefinitionVersion;
        WorkflowStateId = workflowStateId;
    }

    public string WorkflowDefinitionId { get; set; }
    public int WorkflowDefinitionVersion { get; set; }
    public string WorkflowStateId { get; set; }
}