using Elsa.Telnyx.Payloads.Abstract;

namespace Elsa.Telnyx.Attributes
{
    /// <summary>
    /// Contains metadata about the activity descriptor to yield from the annotated payload.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class WebhookAttribute : Attribute
    {
        /// <inheritdoc />
        public WebhookAttribute(string eventType, string activityType, string displayName, string description)
        {
            EventType = eventType;
            ActivityType = activityType;
            DisplayName = displayName;
            Description = description;
        }

        /// <summary>
        /// The Telnyx event to match to th annotated <see cref="Payload"/> type.
        /// </summary>
        public string EventType { get; }
        
        /// <summary>
        /// The activity type name to yield for the annotated <see cref="Payload"/> type. 
        /// </summary>
        public string ActivityType { get; }
        
        /// <summary>
        /// The activity display name to yield for the annotated <see cref="Payload"/> type. 
        /// </summary>
        public string DisplayName { get; }
        
        /// <summary>
        /// The activity description to yield for the annotated <see cref="Payload"/> type. 
        /// </summary>
        public string Description { get; }
    }
}