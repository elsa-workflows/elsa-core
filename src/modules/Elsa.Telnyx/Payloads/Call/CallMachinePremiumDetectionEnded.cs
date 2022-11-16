using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallMachinePremiumDetectionEnded, ActivityTypeName, "Call Machine Premium Detection Ended", "Triggered when machine detection has ended.")]
public sealed record CallMachinePremiumDetectionEnded : CallMachineDetectionEndedBase
{
    public const string ActivityTypeName = nameof(CallMachinePremiumDetectionEnded);
}