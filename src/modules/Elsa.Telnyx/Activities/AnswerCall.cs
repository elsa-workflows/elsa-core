using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Services;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.
/// </summary>
[Activity(Constants.Namespace, "Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.", Kind = ActivityKind.Task)]
[FlowNode("Connected", "Disconnected")]
[WebhookDriven(WebhookEventTypes.CallAnswered)]
public class AnswerCall : ActivityBase<CallAnsweredPayload>, IBookmarksPersistedHandler
{
    /// <summary>
    /// The call control ID to answer. Leave blank when the workflow is driven by an incoming call and you wish to pick up that one.
    /// </summary>
    public Input<string?>? CallControlId { get; set; }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        // Create a bookmark first, then after it's persisted, we call out to Telnyx.
        // This ensures that the bookmark is available in case Telnyx responds with the webhook before the runtime got a chance to persist bookmarks.
        context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallAnswered), ResumeAsync);
    }

    /// <summary>
    /// Invokes Telnyx to answer the call.
    /// </summary>
    /// <param name="context"></param>
    public async ValueTask BookmarksPersistedAsync(ActivityExecutionContext context) => await InvokeTelnyxAsync(context);
    
    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetInput<CallAnsweredPayload>();
        context.Set(Result, payload);
        await context.CompleteActivityAsync(new Outcomes("Connected"));
    }

    /// <summary>
    /// Invokes Telnyx' API to answer the call.
    /// </summary>
    private async ValueTask InvokeTelnyxAsync(ActivityExecutionContext context)
    {
        var callControlId = context.GetCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
        var request = new AnswerCallRequest();
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();
        
        try
        {
            await telnyxClient.Calls.AnswerCallAsync(callControlId, request, context.CancellationToken);    
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await context.CompleteActivityAsync(new Outcomes("Disconnected"));
        }
    }
}