using Elsa.Expressions.Models;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;

namespace Elsa.Telnyx.Activities;

[Activity(
    "Telnyx",
    "Telnyx",
    "Triggered when an inbound phone call is received for any of the specified source or destination phone numbers.",
    Kind = ActivityKind.Trigger)]
public class IncomingCall : Trigger<CallInitiatedPayload>
{
    [Input(Description = "A list of destination numbers to respond to.", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> To { get; set; } = default!;

    [Input(Description = "A list of source numbers to respond to.", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> From { get; set; } = default!;

    [Input(Description = "Match any inbound calls")]
    public Input<bool> CatchAll { get; set; } = default!;

    /// <inheritdoc />
    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context) => GetBookmarkPayloads(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (context.IsTriggerOfWorkflow())
        {
            await ResumeAsync(context);
        }
        else
        {
            var bookmarkPayloads = GetBookmarkPayloads(context.ExpressionExecutionContext);
            foreach (var bookmarkPayload in bookmarkPayloads) context.CreateBookmark(bookmarkPayload, ResumeAsync);
        }
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var webhookModel = context.GetInput<TelnyxWebhook>(WebhookSerializerOptions.Create());
        var callInitiatedPayload = (CallInitiatedPayload)webhookModel.Data.Payload;

        // Correlate workflow with call session ID.
        // TODO: Add support for multiple correlation ID keys.
        context.WorkflowExecutionContext.CorrelationId = callInitiatedPayload.CallSessionId;
            
        // Associate workflow with inbound call control ID.
        context.SetMainCallControlId(callInitiatedPayload.CallControlId);
            
        // Store webhook payload as output.
        context.Set(Result, callInitiatedPayload);

        await context.CompleteActivityAsync();
    }
        
    private IEnumerable<object> GetBookmarkPayloads(ExpressionExecutionContext context)
    {
        var from = context.Get(From) ?? ArraySegment<string>.Empty;
        var to = context.Get(To) ?? ArraySegment<string>.Empty;
        var catchAll = context.Get(CatchAll);
                
        foreach (var phoneNumber in from) yield return new IncomingCallFromBookmarkPayload(phoneNumber);
        foreach (var phoneNumber in to) yield return new IncomingCallToBookmarkPayload(phoneNumber);

        if (catchAll)
            yield return new IncomingCallCatchAllBookmarkPayload();
    }
}