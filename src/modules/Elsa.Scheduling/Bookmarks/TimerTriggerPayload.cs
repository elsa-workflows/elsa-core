namespace Elsa.Scheduling.Bookmarks;

internal record TimerTriggerPayload(DateTimeOffset StartAt, TimeSpan Interval);