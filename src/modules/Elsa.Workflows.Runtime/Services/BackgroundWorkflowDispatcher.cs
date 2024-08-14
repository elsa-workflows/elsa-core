using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

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
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowDefinitionCommand(request.DefinitionVersionId)
        {
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId,
            InstanceId = request.InstanceId,
            TriggerActivityId = request.TriggerActivityId
        };

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowInstanceCommand(request.InstanceId){
            BookmarkId = request.BookmarkId,
            ActivityHandle = request.ActivityHandle,
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId};

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
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
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var command = new DispatchResumeWorkflowsCommand(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        };
        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }
}