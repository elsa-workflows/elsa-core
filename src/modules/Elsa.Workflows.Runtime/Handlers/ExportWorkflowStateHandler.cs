using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Handlers;

public class ExportWorkflowStateHandler : INotificationHandler<WorkflowExecuted>
{
    private readonly IWorkflowStateExporter _exporter;

    public ExportWorkflowStateHandler(IWorkflowStateExporter exporter)
    {
        _exporter = exporter;
    }
    
    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        await _exporter.ExportAsync(notification.Workflow, notification.WorkflowState, cancellationToken);
    }
}