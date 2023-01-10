using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call;

[Webhook(EventType, ActivityTypeName, "Call Machine Premium Greeting Ended", "Triggered when a machine greeting has ended.")]
public sealed record CallMachinePremiumGreetingEnded : CallMachineGreetingEndedBase
{
    public const string EventType = "call.machine.premium.greeting.ended";
    public const string ActivityTypeName = nameof(CallMachinePremiumGreetingEnded);
}