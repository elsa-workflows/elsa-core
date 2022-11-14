using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call
{
    [Webhook(EventType, ActivityTypeName, "Call Machine Greeting Ended", "Triggered when a machine greeting has ended.")]
    public sealed record CallMachineGreetingEnded : CallMachineGreetingEndedBase
    {
        public const string EventType = "call.machine.greeting.ended";
        public const string ActivityTypeName = "CallMachineGreetingEnded";
    }
}