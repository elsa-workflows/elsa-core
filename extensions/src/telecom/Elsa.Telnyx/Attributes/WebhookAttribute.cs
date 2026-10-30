using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Attributes;

/// <summary>
/// Used to handle a Telnyx webhook event.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class WebhookAttribute : Attribute
{
    /// <inheritdoc />
    public WebhookAttribute(string eventType)
    {
        EventType = eventType;
    }

    /// <summary>
    /// The Telnyx event to match to th annotated <see cref="Payload"/> type.
    /// </summary>
    public string EventType { get; }
}