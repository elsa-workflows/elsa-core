using Elsa.MassTransit.Messages;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
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
        await _bus.Publish<DispatchWorkflowDefinition>(new
        {
            request.DefinitionId,
            request.Input,
            request.CorrelationId,
            request.VersionOptions
        }, cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Publish<DispatchWorkflowInstance>(new
        {
            request.Input,
            request.ActivityId,
            request.BookmarkId,
            request.CorrelationId,
            request.InstanceId
        }, cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchTriggerWorkflowsResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Publish<DispatchTriggerWorkflows>(new
        {
            request.Input,
            request.BookmarkPayload,
            request.CorrelationId,
            request.ActivityTypeName
        }, cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<DispatchResumeWorkflowsResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Publish<DispatchResumeWorkflows>(new
        {
            request.Input,
            request.BookmarkPayload,
            request.CorrelationId,
            request.ActivityTypeName
        }, cancellationToken);
        return new();
    }
}