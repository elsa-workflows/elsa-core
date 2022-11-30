using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Exceptions;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Options;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Dial a number or SIP URI.
/// </summary>
[Activity(Constants.Namespace, "Dial a number or SIP URI.", Kind = ActivityKind.Task)]
[WebhookDriven(WebhookEventTypes.CallAnswered, WebhookEventTypes.CallHangup, WebhookEventTypes.CallMachineGreetingEnded, WebhookEventTypes.CallMachinePremiumGreetingEnded)]
public abstract class DialBase : ActivityBase
{
    [Input(Description = "The DID or SIP URI to dial out and bridge to the given call.")]
    public Input<string> To { get; set; } = default!;

    [Input(Description = "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.")]
    public Input<string?> From { get; set; } = default!;

    [Input(Description =
        "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.")]
    public Input<string?> FromDisplayName { get; set; } = default!;

    [Input(
        Description = "Enables Answering Machine Detection.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "disabled", "detect", "detect_beep", "detect_words", "greeting_end", "premium" },
        DefaultValue = "disabled")]
    public Input<string?> AnsweringMachineDetection { get; set; } = new("disabled");

    public Output<string?> MachineDetectionResult { get; set; } = default!;
    public Output<CallHangupPayload>? HangupPayload { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await DialAsync(context);
        var eventTypes = new[] { WebhookEventTypes.CallAnswered, WebhookEventTypes.CallHangup, WebhookEventTypes.CallMachineGreetingEnded, WebhookEventTypes.CallMachinePremiumGreetingEnded };
        var payloads = eventTypes.Select(x => new WebhookEventBookmarkPayload(x)).ToList();
        context.CreateBookmarks(payloads, ResumeAsync);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetInput<CallPayload>();

        if (!context.HasPrimaryCallControlId())
            context.SetPrimaryCallControlId(payload.CallControlId);
        else
            context.SetSecondaryCallControlId(payload.CallControlId);

        switch (payload)
        {
            case CallMachineGreetingEnded greetingEndedPayload:
                await HandleMachineGreetingEnded(context, greetingEndedPayload);
                break;
            case CallMachinePremiumGreetingEnded premiumGreetingEndedPayload:
                await HandleMachineGreetingEnded(context, premiumGreetingEndedPayload);
                break;
            case CallAnsweredPayload answeredPayload:
                await HandleAnsweredAsync(context, answeredPayload);
                break;
            case CallHangupPayload hangupPayload:
                context.Set(HangupPayload, hangupPayload);
                await HandleHangupAsync(context, hangupPayload);
                break;
        }
    }

    private async ValueTask HandleMachineGreetingEnded(ActivityExecutionContext context, CallMachineGreetingEndedBase payload)
    {
        context.Set(MachineDetectionResult, payload.Result);
        await OnHandleMachineGreetingEndedAsync(context, payload);
    }
    
    private ValueTask HandleAnsweredAsync(ActivityExecutionContext context, CallAnsweredPayload payload) => OnHandleAnsweredAsync(context, payload);
    private ValueTask HandleHangupAsync(ActivityExecutionContext context, CallHangupPayload payload) => OnHandleHangupAsync(context, payload);
    
    protected abstract ValueTask OnHandleAnsweredAsync(ActivityExecutionContext context, CallAnsweredPayload payload);
    protected abstract ValueTask OnHandleHangupAsync(ActivityExecutionContext context, CallHangupPayload payload);
    protected abstract ValueTask OnHandleMachineGreetingEndedAsync(ActivityExecutionContext context, CallMachineGreetingEndedBase payload);

    private async Task DialAsync(ActivityExecutionContext context)
    {
        var telnyxOptions = context.GetRequiredService<IOptions<TelnyxOptions>>().Value;
        var callControlAppId = telnyxOptions.CallControlAppId;

        if (callControlAppId == null)
            throw new MissingCallControlAppIdException("No Call Control ID specified and no default value configured");

        var fromNumber = context.Get(From);
        var clientState = context.CreateCorrelatingClientState();

        var request = new DialRequest(
            callControlAppId,
            To.Get(context) ?? throw new Exception("To is required"),
            fromNumber,
            FromDisplayName.Get(context).SanitizeCallerName(),
            AnsweringMachineDetection.Get(context),
            ClientState: clientState
        );

        var telnyxClient = context.GetRequiredService<ITelnyxClient>();
        await telnyxClient.Calls.DialAsync(request, context.CancellationToken);
    }
}

[FlowNode("Answered", "Hangup", "Voicemail")]
public class FlowDial : DialBase
{
    protected override async ValueTask OnHandleAnsweredAsync(ActivityExecutionContext context, CallAnsweredPayload payload) => await context.CompleteActivityWithOutcomesAsync("Answered");
    protected override async ValueTask OnHandleHangupAsync(ActivityExecutionContext context, CallHangupPayload payload) => await context.CompleteActivityWithOutcomesAsync("Hangup");
    protected override async ValueTask OnHandleMachineGreetingEndedAsync(ActivityExecutionContext context, CallMachineGreetingEndedBase payload) => await context.CompleteActivityWithOutcomesAsync("Voicemail");
}

public class Dial : DialBase
{
    [Port] public IActivity? Answered { get; set; }
    [Port] public IActivity? Hangup { get; set; }
    [Port] public IActivity? Voicemail { get; set; }

    protected override async ValueTask OnHandleAnsweredAsync(ActivityExecutionContext context, CallAnsweredPayload payload)
    {
        await context.ScheduleActivityAsync(Answered);
    }

    protected override async ValueTask OnHandleHangupAsync(ActivityExecutionContext context, CallHangupPayload payload)
    {
        await context.ScheduleActivityAsync(Hangup);
    }

    protected override async ValueTask OnHandleMachineGreetingEndedAsync(ActivityExecutionContext context, CallMachineGreetingEndedBase payload)
    {
        await context.ScheduleActivityAsync(Voicemail);
    }
}