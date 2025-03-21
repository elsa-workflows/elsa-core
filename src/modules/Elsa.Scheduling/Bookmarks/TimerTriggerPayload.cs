namespace Elsa.Scheduling.Bookmarks;

public record TimerTriggerPayload(DateTimeOffset StartAt, TimeSpan Interval);