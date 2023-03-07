using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Handlers;

// ReSharper disable once UnusedType.Global
internal class ExportWorkflowStateHandler : INotificationHandler<WorkflowExecuted>
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