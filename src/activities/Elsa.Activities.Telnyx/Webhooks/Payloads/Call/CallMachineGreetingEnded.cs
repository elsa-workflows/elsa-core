using Elsa.Activities.Telnyx.Webhooks.Attributes;

namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Machine Greeting Ended", "Triggered when a machine greeting has ended.")]
    public sealed record CallMachineGreetingEnded : CallPayload
    {
        public const string EventType = "call.machine.greeting.ended";
        public const string ActivityTypeName = "CallMachineGreetingEnded";
    }
}