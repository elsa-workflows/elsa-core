using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Payloads.Call;

/// <summary>
/// Represents the payload of a webhook event that is triggered when an incoming call is answered.
/// </summary>
[Webhook(WebhookEventTypes.CallAnswered)]
public sealed record CallAnsweredPayload : CallPayload
{
    /// <summary>
    /// The from number.
    /// </summary>
    public string From { get; init; } = default!;
    
    /// <summary>
    /// The to number.
    /// </summary>
    public string To { get; init; } = default!;
    
    /// <summary>
    /// Call state.
    /// </summary>
    public string State { get; init; } = default!;
}