using System.Threading;
using System.Threading.Tasks;
using NodaTime;

namespace Elsa.Activities.Temporal.Common.Services
{
    public interface IWorkflowDefinitionScheduler
    {
        Task ScheduleAsync(string workflowDefinitionId, string activityId, Instant startAt, Duration? interval, CancellationToken cancellationToken = default);
        Task ScheduleAsync(string workflowDefinitionId, string activityId, string cronExpression, CancellationToken cancellationToken = default);
        Task UnscheduleAsync(string workflowDefinitionId, string activityId, CancellationToken cancellationToken = default);
        Task UnscheduleAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
        Task UnscheduleAllAsync(CancellationToken cancellationToken = default);
    }
}