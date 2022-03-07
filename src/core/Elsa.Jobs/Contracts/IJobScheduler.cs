using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Jobs.Contracts;

public interface IJobScheduler
{
    Task ScheduleAsync(IJob job, ISchedule schedule, string[]? groupKeys = default, CancellationToken cancellationToken = default);
    Task UnscheduleAsync(IJob job, CancellationToken cancellationToken = default);
    Task ClearAsync(string[]? groupKeys = default, CancellationToken cancellationToken = default);
}