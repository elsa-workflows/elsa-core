using Elsa.Jobs.Services;

namespace Elsa.Jobs.Schedules;

public record RecurringSchedule(DateTimeOffset StartAt, TimeSpan Interval) : ISchedule;