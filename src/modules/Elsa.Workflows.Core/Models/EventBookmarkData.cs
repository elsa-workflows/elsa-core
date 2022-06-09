namespace Elsa.Workflows.Core.Models;

public class EventBookmarkData
{
    private readonly string _eventName = default!;

    public EventBookmarkData(string eventName)
    {
        EventName = eventName;
    }

    public string EventName
    {
        get => _eventName;
        init => _eventName = value.ToLowerInvariant();
    }
}