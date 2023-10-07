using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Triggered when an inbound phone call is received for any of the specified source or destination phone numbers.
/// </summary>
[Activity(
    "Telnyx",
    "Telnyx",
    "Triggered when an inbound phone call is received for any of the specified source or destination phone numbers.",
    Kind = ActivityKind.Trigger)]
public class IncomingCall : Trigger<CallInitiatedPayload>
{
    /// <inheritdoc />
    public IncomingCall([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// A list of destination numbers to respond to.
    /// </summary>
    [Input(Description = "A list of destination numbers to respond to.", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> To { get; set; } = default!;

    /// <summary>
    /// A list of source numbers to respond to.
    /// </summary>
    [Input(Description = "A list of source numbers to respond to.", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> From { get; set; } = default!;

    /// <summary>
    /// Match any inbound calls.
    /// </summary>
    [Input(Description = "Match any inbound calls.")]
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
        var webhookModel = context.GetWorkflowInput<TelnyxWebhook>(WebhookSerializerOptions.Create());
        var callInitiatedPayload = (CallInitiatedPayload)webhookModel.Data.Payload;

        // Store webhook payload as output.
        Result.Set(context, callInitiatedPayload);

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