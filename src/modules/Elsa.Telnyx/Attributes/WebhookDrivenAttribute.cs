namespace Elsa.Telnyx.Attributes;

/// <summary>
/// Contains metadata about the activity descriptor to yield from the annotated payload.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class WebhookDrivenAttribute : Attribute
{
    /// <inheritdoc />
    public WebhookDrivenAttribute(params string[] eventTypes)
    {
        EventTypes = new HashSet<string>(eventTypes);
    }

    /// <summary>
    /// The Telnyx event to match.
    /// </summary>
    public ISet<string> EventTypes { get; }
}