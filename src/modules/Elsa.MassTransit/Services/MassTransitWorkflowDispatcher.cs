using Elsa.MassTransit.Contracts;
using Elsa.MassTransit.Messages;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using MassTransit;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Implements <see cref="IWorkflowDispatcher"/> by leveraging MassTransit.
/// </summary>
public class MassTransitWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly IBus _bus;
    private readonly IEndpointChannelFormatter _endpointChannelFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitWorkflowDispatcher"/> class.
    /// </summary>
    public MassTransitWorkflowDispatcher(IBus bus, IEndpointChannelFormatter endpointChannelFormatter)
    {
        _bus = bus;
        _endpointChannelFormatter = endpointChannelFormatter;
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
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
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
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
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);
        await sendEndpoint.Send(new DispatchTriggerWorkflows(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        }, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await GetSendEndpointAsync(options);
        await sendEndpoint.Send(new DispatchResumeWorkflows(request.ActivityTypeName, request.BookmarkPayload)
        {
            CorrelationId = request.CorrelationId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            ActivityInstanceId = request.ActivityInstanceId,
            Input = request.Input
        }, cancellationToken);
        return DispatchWorkflowResponse.Success();
    }
    
    private async Task<ISendEndpoint> GetSendEndpointAsync(DispatchWorkflowOptions? options = default)
    {
        var endpointName = _endpointChannelFormatter.FormatEndpointName(options?.Channel);
        var sendEndpoint = await _bus.GetSendEndpoint(new Uri($"queue:{endpointName}"));
        return sendEndpoint;
    }
}