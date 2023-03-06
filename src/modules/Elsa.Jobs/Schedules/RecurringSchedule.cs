using Elsa.Jobs.Contracts;

namespace Elsa.Jobs.Schedules;

public record RecurringSchedule(DateTimeOffset StartAt, TimeSpan Interval) : ISchedule;