using Elsa.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Api.Endpoints.Events.Trigger;

public class Trigger : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IHasher _hasher;

    public Trigger(IWorkflowRuntime workflowRuntime, IHasher hasher)
    {
        _workflowRuntime = workflowRuntime;
        _hasher = hasher;
    }

    public override void Configure()
    {
        Post("/events/{eventName}/trigger");
        ConfigurePermissions("trigger:event");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var eventBookmark = new EventBookmarkPayload(request.EventName);
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<Event>();
        var result = await _workflowRuntime.TriggerWorkflowsAsync(bookmarkName, eventBookmark, new TriggerWorkflowsOptions(), cancellationToken);
        
        if (!HttpContext.Response.HasStarted)
        {
            await SendOkAsync(cancellationToken);
        }
    }
}
