using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// A simple implementation that queues the specified request for workflow execution on a non-durable background worker.
/// </summary>
public class BackgroundWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly ICommandSender _commandSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public BackgroundWorkflowDispatcher(ICommandSender commandSender)
    {
        _commandSender = commandSender;
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

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
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
        
        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchWorkflowInstanceResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchTriggerWorkflowsResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchTriggerWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload, request.CorrelationId, request.WorkflowInstanceId, request.Input);
        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchTriggerWorkflowsResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchResumeWorkflowsResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchResumeWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload, request.CorrelationId, request.Input);
        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchResumeWorkflowsResponse();
    }
}