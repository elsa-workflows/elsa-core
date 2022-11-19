using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

/// <summary>
/// A simple implementation that queues the specified request for workflow execution on a non-durable background worker.
/// </summary>
public class TaskBasedWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly IBackgroundCommandSender _backgroundCommandSender;

    public TaskBasedWorkflowDispatcher(IBackgroundCommandSender backgroundCommandSender)
    {
        _backgroundCommandSender = backgroundCommandSender;
    }
    
    public async Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowDefinition(
            request.DefinitionId, 
            request.VersionOptions, 
            request.Input,
            request.CorrelationId);
        
        await _backgroundCommandSender.SendAsync(command, cancellationToken);
        return new DispatchWorkflowDefinitionResponse();
    }

    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowInstance(request.InstanceId, request.BookmarkId, request.ActivityId, request.Input, request.CorrelationId);
        await _backgroundCommandSender.SendAsync(command, cancellationToken);
        return new DispatchWorkflowInstanceResponse();
    }
}