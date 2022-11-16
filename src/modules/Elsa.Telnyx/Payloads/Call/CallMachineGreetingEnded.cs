using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[Webhook(WebhookEventTypes.CallMachineGreetingEnded, ActivityTypeName, "Call Machine Greeting Ended", "Triggered when a machine greeting has ended.")]
public sealed record CallMachineGreetingEnded : CallMachineGreetingEndedBase
{
    public const string ActivityTypeName = "CallMachineGreetingEnded";
}