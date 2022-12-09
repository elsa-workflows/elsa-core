using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(
    WebhookEventTypes.CallMachinePremiumDetectionEnded, 
    WebhookActivityTypeNames.CallMachinePremiumDetectionEnded, 
    "Call Machine Premium Detection Ended", 
    "Triggered when machine detection has ended."
)]
public sealed record CallMachinePremiumDetectionEnded : CallMachineDetectionEndedBase
{
}