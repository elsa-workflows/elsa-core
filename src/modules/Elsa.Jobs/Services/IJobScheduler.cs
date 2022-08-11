using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Jobs.Services;

public interface IJobScheduler
{
    Task ScheduleAsync(IJob job, string name, ISchedule schedule, string[]? groupKeys = default, CancellationToken cancellationToken = default);
    Task UnscheduleAsync(string name, CancellationToken cancellationToken = default);
    Task ClearAsync(string[]? groupKeys = default, CancellationToken cancellationToken = default);
}