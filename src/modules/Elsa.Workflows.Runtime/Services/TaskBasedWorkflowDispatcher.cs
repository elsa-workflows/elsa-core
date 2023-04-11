using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A simple implementation that queues the specified request for workflow execution on a non-durable background worker.
/// </summary>
public class TaskBasedWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly IBackgroundCommandSender _backgroundCommandSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TaskBasedWorkflowDispatcher(IBackgroundCommandSender backgroundCommandSender)
    {
        _backgroundCommandSender = backgroundCommandSender;
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowDefinitionCommand(
            request.DefinitionId,
            request.VersionOptions,
            request.Input,
            request.CorrelationId,
            request.InstanceId,
            request.TriggerActivityId);

        await _backgroundCommandSender.SendAsync(command, cancellationToken);
        return new DispatchWorkflowDefinitionResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowInstanceCommand(
            request.InstanceId, 
            request.BookmarkId, 
            request.ActivityId,
            request.ActivityNodeId,
            request.ActivityInstanceId,
            request.ActivityHash,
            request.Input, 
            request.CorrelationId);
        
        await _backgroundCommandSender.SendAsync(command, cancellationToken);
        return new DispatchWorkflowInstanceResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchTriggerWorkflowsResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchTriggerWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload, request.CorrelationId, request.WorkflowInstanceId, request.Input);
        await _backgroundCommandSender.SendAsync(command, cancellationToken);
        return new DispatchTriggerWorkflowsResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchResumeWorkflowsResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchResumeWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload, request.CorrelationId, request.Input);
        await _backgroundCommandSender.SendAsync(command, cancellationToken);
        return new DispatchResumeWorkflowsResponse();
    }
}