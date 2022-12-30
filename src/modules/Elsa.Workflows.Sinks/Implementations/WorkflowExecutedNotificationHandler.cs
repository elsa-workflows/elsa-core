using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Services;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Models;

namespace Elsa.Workflows.Sinks.Implementations;

public class WorkflowExecutedNotificationHandler : INotificationHandler<WorkflowExecuted>
{
    private readonly ITransport<ExportWorkflowSinkMessage> _transport;

    public WorkflowExecutedNotificationHandler(ITransport<ExportWorkflowSinkMessage> transport)
    {
        _transport = transport;
    }
    
    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        await _transport.SendAsync(new ExportWorkflowSinkMessage(notification.WorkflowState), cancellationToken);
    }
}