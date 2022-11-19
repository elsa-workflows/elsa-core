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
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Dial a number or SIP URI.
/// </summary>
[Activity(Constants.Namespace, "Dial a number or SIP URI.", Kind = ActivityKind.Task)]
[FlowNode("Answered", "Hangup")]
public class Dial : ActivityBase
{
    [Input(Description = "The DID or SIP URI to dial out and bridge to the given call.")]
    public Input<string?>? To { get; set; } = default!;

    [Input(Description = "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.")]
    public Input<string?>? From { get; set; }

    [Input(Description =
        "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.")]
    public Input<string?>? FromDisplayName { get; set; }

    [Input(
        Description = "Enables Answering Machine Detection.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "disabled", "detect", "detect_beep", "detect_words", "greeting_end" },
        DefaultValue = "disabled")]
    public Input<string?>? AnsweringMachineDetection { get; set; } = new("disabled");

    public Output<string?>? MachineDetectionResult { get; set; }


    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await DialAsync(context);
        context.CreateBookmark(ResumeAsync);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetInput<CallPayload>();

        if (!context.HasMainCallControlId())
            context.SetMainCallControlId(payload.CallControlId);

        switch (payload)
        {
            case CallMachineGreetingEnded greetingEndedPayload:
                HandleMachineGreetingEnded(context, greetingEndedPayload);
                break;
            case CallMachinePremiumGreetingEnded premiumGreetingEndedPayload:
                HandleMachineGreetingEnded(context, premiumGreetingEndedPayload);
                break;
            case CallAnsweredPayload:
                await HandleAnsweredAsync(context);
                break;
            case CallHangupPayload:
                await HandleHangupAsync(context);
                break;
        }
    }

    private async ValueTask HandleAnsweredAsync(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Answered");

    private async ValueTask HandleHangupAsync(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Hangup"); 

    private void HandleMachineGreetingEnded(ActivityExecutionContext context, CallMachineGreetingEndedBase payload)
    {
        context.Set(MachineDetectionResult, payload.Result);
        context.CreateBookmark();
    }

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