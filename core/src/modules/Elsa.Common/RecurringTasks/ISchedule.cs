using Microsoft.Extensions.Logging;

namespace Elsa.Common.RecurringTasks;

public interface ISchedule
{
    ScheduledTimer CreateTimer(Func<Task> action, ILogger? logger = null);
}