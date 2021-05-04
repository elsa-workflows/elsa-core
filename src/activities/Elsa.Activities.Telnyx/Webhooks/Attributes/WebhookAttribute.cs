using System;

namespace Elsa.Activities.Telnyx.Webhooks.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WebhookAttribute : Attribute
    {
        public WebhookAttribute(string eventType, string activityType, string displayName, string description)
        {
            EventType = eventType;
            ActivityType = activityType;
            DisplayName = displayName;
            Description = description;
        }
        
        public string EventType { get; }
        public string ActivityType { get; }
        public string DisplayName { get; }
        public string Description { get; }
    }
}