using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallMachinePremiumGreetingEnded, ActivityTypeName, "Call Machine Premium Greeting Ended", "Triggered when a machine greeting has ended.")]
public sealed record CallMachinePremiumGreetingEnded : CallMachineGreetingEndedBase
{
    public const string ActivityTypeName = nameof(CallMachinePremiumGreetingEnded);
}