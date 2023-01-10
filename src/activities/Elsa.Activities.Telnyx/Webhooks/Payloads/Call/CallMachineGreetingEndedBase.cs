namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call;

public record CallMachineGreetingEndedBase : CallPayload
{
    public string Result { get; set; } = default!;
}