using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Models;

namespace Elsa.Workflows.Sinks.Implementations;

public class WorkflowExecutedNotificationHandler : INotificationHandler<WorkflowExecuted>
{
    private readonly ISinkTransport _sinkTransport;

    public WorkflowExecutedNotificationHandler(ISinkTransport sinkTransport)
    {
        _sinkTransport = sinkTransport;
    }
    
    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        await _sinkTransport.SendAsync(new ExportWorkflowSinkMessage(notification.WorkflowState), cancellationToken);
    }
}