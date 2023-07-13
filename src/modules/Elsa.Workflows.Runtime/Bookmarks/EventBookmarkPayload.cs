namespace Elsa.Workflows.Core.Models;

public class EventBookmarkPayload
{
    private readonly string _eventName = default!;

    public EventBookmarkPayload(string eventName)
    {
        EventName = eventName;
    }

    public string EventName
    {
        get => _eventName;
        init => _eventName = value.ToLowerInvariant();
    }
}