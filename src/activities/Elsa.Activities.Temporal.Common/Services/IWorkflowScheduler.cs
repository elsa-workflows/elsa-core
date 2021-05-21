using System.Threading;
using System.Threading.Tasks;
using NodaTime;

namespace Elsa.Activities.Temporal.Common.Services
{
    public interface IWorkflowScheduler
    {
        Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, Instant startAt, Duration? interval = default, CancellationToken cancellationToken = default);
        Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, string cronExpression, CancellationToken cancellationToken = default);
        Task UnscheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, CancellationToken cancellationToken = default);
        Task UnscheduleWorkflowDefinitionAsync(string workflowDefinitionId, string? tenantId, CancellationToken cancellationToken = default);
    }
}