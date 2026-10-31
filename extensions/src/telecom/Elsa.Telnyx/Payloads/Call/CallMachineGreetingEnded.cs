using Elsa.Telnyx.Attributes;

namespace Elsa.Telnyx.Payloads.Call;

[WebhookActivity(WebhookEventTypes.CallMachineGreetingEnded, WebhookActivityTypeNames.CallMachineGreetingEnded, "Call Machine Greeting Ended", "Triggered when a machine greeting has ended.")]
public sealed record CallMachineGreetingEnded : CallMachineGreetingEndedBase;