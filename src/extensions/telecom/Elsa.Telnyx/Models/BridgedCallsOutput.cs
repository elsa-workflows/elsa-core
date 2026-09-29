using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Payloads.Call;

namespace Elsa.Telnyx.Models;

/// <summary>
/// Contains output of the <see cref="BridgeCalls"/> activity.
/// </summary>
/// <param name="PayloadA">The payload from leg A.</param>
/// <param name="PayloadB">The payload from leg B.</param>
public record BridgedCallsOutput(CallBridgedPayload PayloadA, CallBridgedPayload PayloadB);