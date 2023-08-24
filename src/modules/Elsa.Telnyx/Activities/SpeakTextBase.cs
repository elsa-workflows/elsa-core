using Elsa.Extensions;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Convert text to speech and play it back on the call.
/// </summary>
[Activity(Constants.Namespace, "Convert text to speech and play it back on the call.", Kind = ActivityKind.Task)]
[WebhookDriven(WebhookEventTypes.CallSpeakEnded)]
public abstract class SpeakTextBase : Activity
{
    /// <inheritdoc />
    protected SpeakTextBase(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// Unique identifier and token for controlling the call.
    /// </summary>
    [Input(
        DisplayName = "Call Control ID",
        Description = "Unique identifier and token for controlling the call.",
        Category = "Advanced"
    )]
    public Input<string> CallControlId { get; set; } = default!;

    /// <summary>
    /// The language you want spoken.
    /// </summary>
    [Input(
        Description = "The language you want spoken.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "en-US", "en-AU", "nl-NL", "es-ES", "ru-RU" },
        DefaultValue = "en-US"
    )]
    public Input<string> Language { get; set; } = new("en-US");

    /// <summary>
    /// The gender of the voice used to speak back the text.
    /// </summary>
    [Input(
        Description = "The gender of the voice used to speak back the text.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "female", "male" },
        DefaultValue = "female"
    )]
    public Input<string> Voice { get; set; } = new("female");

    /// <summary>
    /// The text or SSML to be converted into speech. There is a 5,000 character limit.
    /// </summary>
    [Input(
        Description = "The text or SSML to be converted into speech. There is a 5,000 character limit.",
        UIHint = InputUIHints.MultiLine
    )]
    public Input<string> Payload { get; set; } = default!;

    /// <summary>
    /// The type of the provided payload. The payload can either be plain text, or Speech Synthesis Markup Language (SSML).
    /// </summary>
    [Input(
        Description = "The type of the provided payload. The payload can either be plain text, or Speech Synthesis Markup Language (SSML).",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "", "text", "ssml" }
    )]
    public Input<string?>? PayloadType { get; set; }

    /// <summary>
    /// This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed.
    /// </summary>
    [Input(
        Description = "This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "", "basic", "premium" },
        Category = "Advanced"
    )]
    public Input<string?> ServiceLevel { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = new SpeakTextRequest(
            Language.GetOrDefault(context) ?? "en-US",
            Voice.GetOrDefault(context) ?? "female",
            Payload.Get(context),
            PayloadType.GetOrDefault(context).EmptyToNull(),
            ServiceLevel.GetOrDefault(context).EmptyToNull(),
            ClientState: context.CreateCorrelatingClientState(context.Id)
        );

        var callControlId = CallControlId.Get(context);
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            // Send the request to Telnyx.
            await telnyxClient.Calls.SpeakTextAsync(callControlId, request, context.CancellationToken);
            
            // Create bookmark to resume the workflow when speaking has finished.
            context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallSpeakEnded, callControlId), HandleSpeakingHasFinished, true);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnected(context);
        }
    }

    /// <summary>
    /// Called when the call was no longer active.
    /// </summary>
    protected abstract ValueTask HandleDisconnected(ActivityExecutionContext context);

    /// <summary>
    /// Called when speaking has finished.
    /// </summary>
    protected abstract ValueTask HandleSpeakingHasFinished(ActivityExecutionContext context);
}