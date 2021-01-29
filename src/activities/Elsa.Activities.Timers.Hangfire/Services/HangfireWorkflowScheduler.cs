using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Hangfire.Models;
using Elsa.Activities.Timers.Services;
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

        public Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, Instant startAt, Duration? interval, CancellationToken cancellationToken)
        {
            var data = CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId, interval?.ToCronExpression());

            _backgroundJobClient.ScheduleWorkflow(data, startAt.ToDateTimeOffset());

            return Task.CompletedTask;
        }
        
        public Task ScheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, string cronExpression, CancellationToken cancellationToken)
        {
            var data = CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId, cronExpression);
            var instant = _crontabParser.GetNextOccurrence(cronExpression);

            _backgroundJobClient.ScheduleWorkflow(data, instant.ToDateTimeOffset());

            return Task.CompletedTask;
        }

        public Task UnscheduleWorkflowAsync(string? workflowDefinitionId, string? workflowInstanceId, string activityId, string? tenantId, CancellationToken cancellationToken = default)
        {
            _backgroundJobClient.UnscheduleJobWhenAlreadyExists(CreateData(workflowDefinitionId, workflowInstanceId, activityId, tenantId));
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