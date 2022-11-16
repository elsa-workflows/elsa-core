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
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.
/// </summary>
[Activity(Constants.Namespace, "Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.", Kind = ActivityKind.Task)]
[FlowNode("Connected", "Disconnected")]
[WebhookDriven(WebhookEventTypes.CallAnswered)]
public class AnswerCall : ActivityBase<CallAnsweredPayload>
{
    /// <inheritdoc />
    public AnswerCall()
    {
        // To avoid Telnyx sending a webhook before the workflow got a chance to persist bookmarks, we wait calling out to Telnyx until after.
        OnSignalReceived<BookmarksPersistedSignal>(InvokeTelnyxAsync);
    }

    /// <summary>
    /// The call control ID to answer. Leave blank when the workflow is driven by an incoming call and you wish to pick up that one.
    /// </summary>
    public Input<string?>? CallControlId { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallAnswered), ResumeAsync);
    }
    
    /// <summary>
    /// Invokes Telnyx' API to answer the call.
    /// </summary>
    private async ValueTask InvokeTelnyxAsync(BookmarksPersistedSignal signal, SignalContext context)
    {
        var activityExecutionContext = context.ReceiverActivityExecutionContext;
        var callControlId = activityExecutionContext.GetCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
        var request = new AnswerCallRequest();
        var telnyxClient = activityExecutionContext.GetRequiredService<ITelnyxClient>();
        
        try
        {
            await telnyxClient.Calls.AnswerCallAsync(callControlId, request, context.CancellationToken);    
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await activityExecutionContext.CompleteActivityAsync(new Outcomes("Disconnected"));
        }
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetInput<CallAnsweredPayload>();
        context.Set(Result, payload);
        await context.CompleteActivityAsync(new Outcomes("Connected"));
    }
}