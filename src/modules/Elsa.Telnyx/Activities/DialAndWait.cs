using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Exceptions;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Options;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Dial a number or SIP URI.
/// </summary>
[Activity(Constants.Namespace, "Dial a number or SIP URI and wait for an event.", Kind = ActivityKind.Task)]
[FlowNode("Answered", "Hangup")]
[WebhookDriven(WebhookEventTypes.CallAnswered, WebhookEventTypes.CallHangup)]
public class DialAndWait : Activity<CallPayload>
{
    /// <inheritdoc />
    public DialAndWait([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The DID or SIP URI to dial out and bridge to the given call.
    /// </summary>
    [Input(Description = "The DID or SIP URI to dial out and bridge to the given call.")]
    public Input<string> To { get; set; } = default!;

    /// <summary>
    /// The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.
    /// </summary>
    [Input(Description = "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.")]
    public Input<string?> From { get; set; } = default!;

    /// <summary>
    /// The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.
    /// </summary>
    [Input(Description =
        "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.")]
    public Input<string?> FromDisplayName { get; set; } = default!;

    /// <summary>
    /// Enables answering machine detection.
    /// </summary>
    [Input(
        Description = "Enables answering machine detection.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "disabled", "detect", "detect_beep", "detect_words", "greeting_end", "premium" },
        DefaultValue = "disabled")]
    public Input<string?> AnsweringMachineDetection { get; set; } = new("disabled");

    /// <summary>
    /// Start recording automatically after an event. Disabled by default.
    /// </summary>
    [Input(Description = "Start recording automatically after an event. Disabled by default.")]
    public Input<bool> Record { get; set; } = default!;

    /// <summary>
    /// Defines the format of the recording ('wav' or 'mp3') when `record` is specified.
    /// </summary>
    [Input(
        Description = "Defines the format of the recording ('wav' or 'mp3') when `record` is specified.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "wav", "mp3" },
        DefaultValue = "mp3"
    )]
    public Input<string> RecordFormat { get; set; } = new("mp3");

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var response = await DialAsync(context);
        var answeredBookmark = new WebhookEventBookmarkPayload(WebhookEventTypes.CallAnswered, response.CallControlId);
        var hangupBookmark = new WebhookEventBookmarkPayload(WebhookEventTypes.CallHangup, response.CallControlId);
        
        context.CreateBookmark(answeredBookmark, OnCallAnswered, false);
        context.CreateBookmark(hangupBookmark, OnCallHangup, false);
    }

    private async ValueTask OnCallAnswered(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallAnsweredPayload>();
        Result.Set(context, payload);
        await context.CompleteActivityWithOutcomesAsync("Answered");
    }
    
    private async ValueTask OnCallHangup(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallHangupPayload>();
        Result.Set(context, payload);
        await context.CompleteActivityWithOutcomesAsync("Hangup");
    }

    private async Task<DialResponse> DialAsync(ActivityExecutionContext context)
    {
        var telnyxOptions = context.GetRequiredService<IOptions<TelnyxOptions>>().Value;
        var callControlAppId = telnyxOptions.CallControlAppId;

        if (callControlAppId == null)
            throw new MissingCallControlAppIdException("No Call Control ID configured");

        var fromNumber = From.GetOrDefault(context);
        var clientState = context.CreateCorrelatingClientState();

        var request = new DialRequest(
            callControlAppId,
            To.Get(context),
            fromNumber,
            FromDisplayName.GetOrDefault(context).SanitizeCallerName(),
            AnsweringMachineDetection.GetOrDefault(context),
            Record: Record.GetOrDefault(context) ? "record-from-answer" : default,
            RecordFormat: RecordFormat.GetOrDefault(context) ?? "mp3",
            ClientState: clientState
        );

        var telnyxClient = context.GetRequiredService<ITelnyxClient>();
        var response = await telnyxClient.Calls.DialAsync(request, context.CancellationToken);

        return response.Data;
    }
}