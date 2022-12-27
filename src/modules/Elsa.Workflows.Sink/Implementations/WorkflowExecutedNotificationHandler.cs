using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Implementations;

public class WorkflowExecutedNotificationHandler : INotificationHandler<WorkflowExecuted>
{
    private readonly ISinkTransport _sinkTransport;

    public WorkflowExecutedNotificationHandler(ISinkTransport sinkTransport)
    {
        _sinkTransport = sinkTransport;
    }
    
    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var state = notification.WorkflowState;
        
        await _sinkTransport.SendAsync(new ExportWorkflowSinkMessage(state.DefinitionId, state.DefinitionVersion, state.Id), cancellationToken);
    }
}