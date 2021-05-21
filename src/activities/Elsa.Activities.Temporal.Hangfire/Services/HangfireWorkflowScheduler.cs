using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Activities.Temporal.Hangfire.Extensions;
using Elsa.Activities.Temporal.Hangfire.Models;
using Hangfire;
using NodaTime;

namespace Elsa.Activities.Temporal.Hangfire.Services
{
    public class HangfireWorkflowScheduler : IWorkflowScheduler
    {
        private readonly JobManager _jobManager;

        public HangfireWorkflowScheduler(JobManager jobManager)
        {
            _jobManager = jobManager;
        }

        public Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, Instant startAt, Duration? interval, CancellationToken cancellationToken)
        {
            var cron = interval?.ToCronExpression();
            var data = CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId, cron);

            _jobManager.ScheduleJob(data, startAt);

            if (cron != null)
                _jobManager.ScheduleJob(data, cron);

            return Task.CompletedTask;
        }

        public Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, string cronExpression, CancellationToken cancellationToken)
        {
            var data = CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId, cronExpression);

            _jobManager.ScheduleJob(data, cronExpression);

            return Task.CompletedTask;
        }

        public Task UnscheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId);
            var identity = data.GetIdentity();
            _jobManager.UnscheduleJob(identity);
            return Task.CompletedTask;
        }

        public Task UnscheduleWorkflowDefinitionAsync(string workflowDefinitionId, string? tenantId, CancellationToken cancellationToken = default)
        {
            _jobManager.UnscheduleJobs(workflowDefinitionId, tenantId);
            return Task.CompletedTask;
        }

        private RunHangfireWorkflowJobModel CreateData(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, string? cronExpression = null)
        {
            return new(
                workflowDefinitionId,
                workflowInstanceId: workflowInstanceId,
                activityId: activityId,
                tenantId: tenantId,
                cronExpression: cronExpression);
        }
    }
}