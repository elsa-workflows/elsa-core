using Elsa.Jobs.Contracts;

namespace Elsa.Jobs.Schedules;

public class CronSchedule : ISchedule
{
    public string CronExpression { get; set; } = default!;
}