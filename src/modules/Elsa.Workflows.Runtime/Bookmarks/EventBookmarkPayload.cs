namespace Elsa.Workflows.Runtime.Bookmarks;

/// <summary>
/// The payload for an event bookmark.
/// </summary>
public class EventBookmarkPayload
{
    private readonly string _eventName = default!;

    /// <summary>
    /// The payload for an event bookmark.
    /// </summary>
    public EventBookmarkPayload(string eventName)
    {
        EventName = eventName;
    }

    /// <summary>
    /// The name of the event.
    /// </summary>
    public string EventName
    {
        get => _eventName;
        init => _eventName = value.ToLowerInvariant();
    }
}