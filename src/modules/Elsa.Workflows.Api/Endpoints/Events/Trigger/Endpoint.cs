using Elsa.Abstractions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Events.Trigger;

/// <summary>
/// Triggers all workflows that are waiting for the specified event.
/// </summary>
[PublicAPI]
internal class Trigger : ElsaEndpoint<Request>
{
    private readonly IEventPublisher _eventPublisher;

    /// <inheritdoc />
    public Trigger(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/events/{eventName}/trigger");
        ConfigurePermissions("trigger:event");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var input = (IDictionary<string, object>?)request.Input;
        var eventName = request.EventName;
        var correlationId = request.CorrelationId;
        var workflowInstanceId = request.WorkflowInstanceId;
        var workflowExecutionMode = request.WorkflowExecutionMode;

        if(workflowExecutionMode == WorkflowExecutionMode.Asynchronous)
            await _eventPublisher.DispatchAsync(eventName, correlationId, workflowInstanceId, input, cancellationToken);
        else
            await _eventPublisher.PublishAsync(eventName, correlationId, workflowInstanceId, input, cancellationToken);

        if (!HttpContext.Response.HasStarted) 
            await SendOkAsync(cancellationToken);
    }
}
