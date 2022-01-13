using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Scheduling.Contracts;

public interface IJobScheduler
{
    Task ScheduleAsync(IJob job, ISchedule schedule, CancellationToken cancellationToken = default);
}