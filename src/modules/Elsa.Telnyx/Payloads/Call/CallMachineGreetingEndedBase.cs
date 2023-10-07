using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

public record CallMachineGreetingEndedBase : CallPayload
{
    public string Result { get; set; } = default!;
}