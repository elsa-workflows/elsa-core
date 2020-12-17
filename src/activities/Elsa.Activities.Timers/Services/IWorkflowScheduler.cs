using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Timers.Services
{
    public interface IWorkflowScheduler
    {
        Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, Duration interval, CancellationToken cancellationToken = default);
        Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, CancellationToken cancellationToken = default);
        Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, string cronExpression, CancellationToken cancellationToken = default);
        Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string workflowInstanceId, string activityId, Instant startAt, CancellationToken cancellationToken = default);
        Task UnscheduleWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, string activityId, CancellationToken cancellationToken = default);
    }
}