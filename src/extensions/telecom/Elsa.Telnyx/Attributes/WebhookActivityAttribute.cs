using Elsa.Telnyx.Payloads.Abstractions;

namespace Elsa.Telnyx.Attributes;

/// <summary>
/// Contains metadata about the activity descriptor to yield from the annotated payload.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class WebhookActivityAttribute : WebhookAttribute
{
    /// <inheritdoc />
    public WebhookActivityAttribute(string eventType, string activityType, string displayName, string description) : base(eventType)
    {
        ActivityType = activityType;
        DisplayName = displayName;
        Description = description;
    }
        
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