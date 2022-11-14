using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(EventType, ActivityTypeName, "Call Machine Premium Greeting Ended", "Triggered when a machine greeting has ended.")]
public sealed record CallMachinePremiumGreetingEnded : CallMachineGreetingEndedBase
{
    public const string EventType = "call.machine.premium.greeting.ended";
    public const string ActivityTypeName = nameof(CallMachinePremiumGreetingEnded);
}