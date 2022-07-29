using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call;

[Webhook(EventType, ActivityTypeName, "Call Machine Premium Detection Ended", "Triggered when machine detection has ended.")]
public sealed record CallMachinePremiumDetectionEnded : CallMachineDetectionEndedBase
{
    public const string EventType = "call.machine.premium.detection.ended";
    public const string ActivityTypeName = nameof(CallMachinePremiumDetectionEnded);
}