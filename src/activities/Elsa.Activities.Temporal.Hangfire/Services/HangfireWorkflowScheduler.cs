using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Options;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Activities.Temporal.Hangfire.Extensions;
using Elsa.Activities.Temporal.Hangfire.Models;
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

        public Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, Instant startAt, Duration? interval, ClusterMode? clusterMode = default, CancellationToken cancellationToken = default)
        {
            var cronExpression = interval?.ToCronExpression();
            var data = CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId, cronExpression, clusterMode);

            _jobManager.ScheduleJob(data, startAt);

            if (cronExpression != null)
                _jobManager.ScheduleRecurringJob(data, cronExpression);

            return Task.CompletedTask;
        }

        public Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, string cronExpression, ClusterMode? clusterMode = default, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId, cronExpression, clusterMode);

            _jobManager.ScheduleRecurringJob(data, cronExpression);

            return Task.CompletedTask;
        }

        public Task UnscheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId);
            _jobManager.DeleteJob(data);
            return Task.CompletedTask;
        }

        public Task UnscheduleWorkflowDefinitionAsync(string workflowDefinitionId, string? tenantId, CancellationToken cancellationToken = default)
        {
            _jobManager.DeleteJobs(workflowDefinitionId, tenantId);
            return Task.CompletedTask;
        }

        private RunHangfireWorkflowJobModel CreateData(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, string? cronExpression = default, ClusterMode? clusterMode = default)
        {
            return new(
                workflowDefinitionId,
                workflowInstanceId: workflowInstanceId,
                activityId: activityId,
                tenantId: tenantId,
                cronExpression: cronExpression,
                clusterMode: clusterMode);
        }
    }
}