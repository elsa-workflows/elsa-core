using System.Threading;
using System.Threading.Tasks;

using Elsa.Activities.Timers.Hangfire.Models;
using Elsa.Activities.Timers.Services;
using Elsa.Services.Models;

using Hangfire;

using NodaTime;

namespace Elsa.Activities.Timers.Hangfire.Services
{
    public class HangfireWorkflowScheduler : IWorkflowScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ICrontabParser _crontabParser;

        public HangfireWorkflowScheduler(IBackgroundJobClient backgroundJobClient, ICrontabParser crontabParser)
        {
            _backgroundJobClient = backgroundJobClient;
            _crontabParser = crontabParser;
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, Duration interval, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowBlueprint, activityId: activityId, cronExpression: interval.ToCronExpression());

            _backgroundJobClient.ScheduleWorkflow(data, startAt.ToDateTimeOffset());

            return Task.CompletedTask;
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowBlueprint, activityId);

            _backgroundJobClient.ScheduleWorkflow(data, startAt.ToDateTimeOffset());

            return Task.CompletedTask;
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowBlueprint, activityId, cronExpression: cronExpression);
            var instant = _crontabParser.GetNextOccurrence(cronExpression);

            _backgroundJobClient.ScheduleWorkflow(data, instant.ToDateTimeOffset());

            return Task.CompletedTask;
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string workflowInstanceId, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowBlueprint, activityId, workflowInstanceId);

            _backgroundJobClient.ScheduleWorkflow(data, startAt.ToDateTimeOffset());

            return Task.CompletedTask;
        }

        public Task UnscheduleWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, string activityId, CancellationToken cancellationToken = default)
        {
          _backgroundJobClient.UnscheduleJobWhenAlreadyExists(
                CreateData(workflowExecutionContext.WorkflowBlueprint,
                    activityId: activityId,
                    workflowInstanceId: workflowExecutionContext.WorkflowInstance.Id)
               );

            return Task.CompletedTask;
        }

        private RunHangfireWorkflowJobModel CreateData(IWorkflowBlueprint workflowBlueprint, string activityId, string? workflowInstanceId = null, string? cronExpression = null) => CreateData(workflowBlueprint.Id,activityId, workflowInstanceId, workflowBlueprint.TenantId, cronExpression);
        private RunHangfireWorkflowJobModel CreateData(string workflowDefinitionId, string activityId, string? workflowInstanceId = null, string? tenantId = null, string? cronExpression = null)
        {
            return new RunHangfireWorkflowJobModel(
                workflowDefinitionId: workflowDefinitionId,
                activityId: activityId,
                workflowInstanceId: workflowInstanceId,
                tenantId: tenantId,
                cronExpression: cronExpression);
        }

    }
}
