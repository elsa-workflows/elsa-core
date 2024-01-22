using Elsa.MassTransit.Messages;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;
using Humanizer;
using MassTransit;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Implements <see cref="IWorkflowDispatcher"/> by leveraging MassTransit.
/// </summary>
public class MassTransitWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly IBus _bus;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MassTransitWorkflowDispatcher(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);
        
        await sendEndpoint.Send(new DispatchWorkflowDefinition(
            request.DefinitionId,
            request.VersionOptions,
            request.Input,
            request.Properties,
            request.CorrelationId,
            request.InstanceId,
            request.TriggerActivityId
        ), cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);
        
        await sendEndpoint.Send(new DispatchWorkflowInstance(request.InstanceId)
        {
            BookmarkId = request.BookmarkId,
            ActivityId = request.ActivityId,
            ActivityNodeId = request.ActivityNodeId,
            ActivityInstanceId = request.ActivityInstanceId,
            ActivityHash = request.ActivityHash,
            Input = request.Input,
            Properties = request.Properties,
            CorrelationId = request.CorrelationId
        }, cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchTriggerWorkflowsResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);
        await sendEndpoint.Send(new DispatchTriggerWorkflows(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        }, cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchResumeWorkflowsResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);
        await sendEndpoint.Send(new DispatchResumeWorkflows(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        }, cancellationToken);
        return new();
    }
    
    private async Task<ISendEndpoint> GetSendEndpointAsync(DispatchWorkflowOptions? options = default)
    {
        var channelSuffix = !string.IsNullOrEmpty(options?.Channel) ? "-" + options.Channel.Kebaberize() : ""; 
        var sendEndpoint = await _bus.GetSendEndpoint(new Uri($"queue:elsa-dispatch-workflow-request{channelSuffix}"));
        return sendEndpoint;
    }
}