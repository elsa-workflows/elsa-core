using Elsa.Abstractions;
using Elsa.Http.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Events.TriggerAuthenticated;

/// <summary>
/// Triggers all workflows that are waiting for the specified event.
/// </summary>
[PublicAPI]
internal class Trigger : ElsaEndpoint<Request>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpBookmarkProcessor _httpBookmarkProcessor;

    /// <inheritdoc />
    public Trigger(IEventPublisher eventPublisher, IHttpBookmarkProcessor httpBookmarkProcessor)
    {
        _eventPublisher = eventPublisher;
        _httpBookmarkProcessor = httpBookmarkProcessor;
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
        var activityInstanceId = request.ActivityInstanceId;
        var workflowExecutionMode = request.WorkflowExecutionMode;

        if (workflowExecutionMode == WorkflowExecutionMode.Asynchronous)
        {
            await _eventPublisher.DispatchAsync(eventName, correlationId, workflowInstanceId, activityInstanceId, input, cancellationToken);
            await SendOkAsync(cancellationToken);
        }
        else
        {
            var results = await _eventPublisher.PublishAsync(eventName, correlationId, workflowInstanceId, activityInstanceId, input, cancellationToken);

            // Resume any HTTP bookmarks.
            foreach (var result in results)
                await _httpBookmarkProcessor.ProcessBookmarks(new[] { result }, correlationId, default, cancellationToken);

            if (!HttpContext.Response.HasStarted) 
                await SendOkAsync(cancellationToken);
        }
    }
}