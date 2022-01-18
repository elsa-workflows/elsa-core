using Elsa.Scheduling.Contracts;

namespace Elsa.Scheduling.Schedules;

public class CronSchedule : ISchedule
{
    public string CronExpression { get; set; } = default!;
}