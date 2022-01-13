using Elsa.Activities.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Schedules;

public class CronSchedule : ISchedule
{
    public string CronExpression { get; set; } = default!;
}