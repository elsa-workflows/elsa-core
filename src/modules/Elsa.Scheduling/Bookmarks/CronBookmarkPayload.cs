namespace Elsa.Scheduling.Bookmarks;

internal record CronBookmarkPayload(DateTimeOffset ExecuteAt, string CronExpression);