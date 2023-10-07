using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

public record CallMachineDetectionEndedBase : CallPayload
{
    public string Result { get; set; } = default!;
}