namespace Elsa.Activities.Telnyx.Webhooks.Payloads.Call;

public record CallMachineDetectionEndedBase : CallPayload
{
    public string Result { get; set; } = default!;
}