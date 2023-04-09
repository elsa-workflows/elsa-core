using Elsa.MassTransit.Messages;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
using MassTransit;

namespace Elsa.MassTransit.Implementations;

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
    public async Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Publish(new DispatchWorkflowDefinition(
            request.DefinitionId,
            request.VersionOptions,
            request.Input,
            request.CorrelationId,
            request.InstanceId
        ), cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Publish(new DispatchWorkflowInstance(
            request.InstanceId,
            request.BookmarkId,
            request.ActivityId,
            request.ActivityNodeId,
            request.ActivityInstanceId,
            request.ActivityHash,
            request.Input,
            request.CorrelationId
        ), cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchTriggerWorkflowsResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Publish(new DispatchTriggerWorkflows(
            request.ActivityTypeName,
            request.BookmarkPayload,
            request.CorrelationId,
            request.WorkflowInstanceId,
            request.Input
        ), cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchResumeWorkflowsResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Publish(new DispatchResumeWorkflows(
            request.ActivityTypeName,
            request.BookmarkPayload,
            request.CorrelationId,
            request.WorkflowInstanceId,
            request.Input
        ), cancellationToken);
        return new();
    }
}