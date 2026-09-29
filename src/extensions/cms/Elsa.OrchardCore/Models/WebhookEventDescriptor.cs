namespace Elsa.OrchardCore.Models
{
    /// <summary>
    /// Represents a descriptor for a webhook event, including its type and payload information.
    /// </summary>
    public class WebhookEventDescriptor
    {
        /// <summary>
        /// Gets or sets the type of the webhook event.
        /// This typically indicates the category or name of the event being triggered.
        /// </summary>
        public string WebhookEventType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the specific type of the event that the webhook corresponds to.
        /// </summary>
        public string EventType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the .NET type of the payload associated with the webhook event.
        /// This describes the structure of the event's data.
        /// </summary>
        public Type PayloadType { get; set; } = null!;
    }
}