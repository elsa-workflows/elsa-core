using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Api.Endpoints.Events.Trigger;

/// <summary>
/// Triggers all workflows that are waiting for the specified event.
/// </summary>
public class Trigger : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <inheritdoc />
    public Trigger(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
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
        var eventBookmark = new EventBookmarkPayload(request.EventName);
        var options = new TriggerWorkflowsRuntimeOptions(request.CorrelationId);
        await _workflowRuntime.TriggerWorkflowsAsync<Event>(eventBookmark, options, cancellationToken);
        
        if (!HttpContext.Response.HasStarted)
        {
            await SendOkAsync(cancellationToken);
        }
    }
}
