using Elsa.Jobs.Services;

namespace Elsa.Jobs.Schedules;

public class CronSchedule : ISchedule
{
    public string CronExpression { get; set; } = default!;
}