using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Sink.Models;

public class ExportWorkflowSinkMessage : ICommand<Unit>, INotification
{
    public ExportWorkflowSinkMessage(WorkflowState workflowState)
    {
        WorkflowState = workflowState;
    }

    public WorkflowState WorkflowState { get; set; }
}