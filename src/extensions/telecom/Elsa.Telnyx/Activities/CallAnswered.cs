using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Represents a Telnyx webhook event trigger.
/// </summary>
[Activity("Telnyx", "Telnyx", "A Telnyx webhook event that executes when a call is answered.", Kind = ActivityKind.Trigger)]
public class CallAnswered : Activity<CallAnsweredPayload>
{
    /// <inheritdoc />
    public CallAnswered([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// A list of call control IDs to listen for.
    /// </summary>
    [Input(Description = "A list of call control IDs to listen for.", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> CallControlIds { get; set; } = null!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var callControlIds = CallControlIds.Get(context);

        foreach (var callControlId in callControlIds)
        {
            var payload = new CallAnsweredStimulus(callControlId);
            context.CreateBookmark(new()
            {
                Stimulus = payload,
                Callback = Resume,
                BookmarkName = Type,
                IncludeActivityInstanceId = false
            });
        }
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        var input = context.GetWorkflowInput<CallAnsweredPayload>(WebhookSerializerOptions.Create());
        context.Set(Result, input);
        await context.CompleteActivityAsync();
    }
}