using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Sinks.Models;

public class ExportWorkflowSinkMessage : ICommand<Unit>, INotification
{
    public ExportWorkflowSinkMessage(WorkflowState workflowState)
    {
        WorkflowState = workflowState;
    }

    public WorkflowState WorkflowState { get; set; }
}