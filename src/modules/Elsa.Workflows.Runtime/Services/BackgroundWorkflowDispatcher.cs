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
        var command = new DispatchWorkflowDefinitionCommand(request.DefinitionId, request.VersionOptions)
        {
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId,
            InstanceId = request.InstanceId,
            TriggerActivityId = request.TriggerActivityId
        };

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchWorkflowDefinitionResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowInstanceCommand(request.InstanceId){
            BookmarkId = request.BookmarkId,
            ActivityId = request.ActivityId,
            ActivityNodeId = request.ActivityNodeId,
            ActivityInstanceId = request.ActivityInstanceId,
            ActivityHash = request.ActivityHash,
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId};

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchWorkflowInstanceResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchTriggerWorkflowsResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchTriggerWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input,
            Properties = request.Properties
        };
        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchTriggerWorkflowsResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchResumeWorkflowsResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchResumeWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        };
        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchResumeWorkflowsResponse();
    }
}