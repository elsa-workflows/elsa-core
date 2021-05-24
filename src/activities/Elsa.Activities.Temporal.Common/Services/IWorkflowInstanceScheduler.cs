using System.Threading;
using System.Threading.Tasks;
using NodaTime;

namespace Elsa.Activities.Temporal.Common.Services
{
    public interface IWorkflowInstanceScheduler
    {
        Task ScheduleAsync(string workflowInstanceId, string activityId, Instant startAt, Duration? interval, CancellationToken cancellationToken = default);
        Task ScheduleAsync(string workflowInstanceId, string activityId, string cronExpression, CancellationToken cancellationToken = default);
        Task UnscheduleAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default);
        Task UnscheduleAsync(string workflowInstanceId, CancellationToken cancellationToken = default);
        Task UnscheduleAllAsync(CancellationToken cancellationToken = default);
    }
}