namespace Elsa.OrchardCore.Models;

public class WebhookEventDescriptor
{
    public string WebhookEventType { get; set; }
    public string EventType { get; set; }
    public Type PayloadType { get; set; }
}