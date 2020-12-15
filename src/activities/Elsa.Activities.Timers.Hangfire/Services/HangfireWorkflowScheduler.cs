using System;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Activities.Timers.Hangfire.Jobs;
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

        public HangfireWorkflowScheduler(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, Duration interval, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowBlueprint, activityId);
       
            _backgroundJobClient.Schedule(
                () =>  RecurringJob.AddOrUpdate<RunHangfireWorkflowJob>(data.GetIdentity(), job => job.ExecuteAsync(data), interval.ToTimeSpan().ToCronExpression(),null,"default")
                ,startAt.ToDateTimeOffset());

            return Task.CompletedTask;
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowBlueprint, activityId, startAt.ToDateTimeOffset());

            _backgroundJobClient.ScheduleWorkflow(data);

            return Task.CompletedTask;
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowBlueprint, activityId);

            RecurringJob.AddOrUpdate<RunHangfireWorkflowJob>(data.GetIdentity(), job => job.ExecuteAsync(data), cronExpression);

            return Task.CompletedTask;
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string workflowInstanceId, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowBlueprint, activityId, workflowInstanceId, startAt.ToDateTimeOffset());

            _backgroundJobClient.ScheduleWorkflow(data);

            return Task.CompletedTask;
        }    

        private RunHangfireWorkflowJobModel CreateData(IWorkflowBlueprint workflowBlueprint, string activityId, DateTimeOffset? dateTimeOffset = null) => CreateData(workflowBlueprint.TenantId, workflowBlueprint.Id, null, activityId, dateTimeOffset);
        private RunHangfireWorkflowJobModel CreateData(IWorkflowBlueprint workflowBlueprint, string activityId, string workflowInstanceId, DateTimeOffset? dateTimeOffset = null) => CreateData(workflowBlueprint.TenantId, workflowBlueprint.Id, workflowInstanceId, activityId, dateTimeOffset);
       
        private RunHangfireWorkflowJobModel CreateData(string? tenantId, string workflowDefinitionId, string? workflowInstanceId, string activityId, DateTimeOffset? dateTimeOffset = null)
        {
            return new RunHangfireWorkflowJobModel(
                workflowDefinitionId: workflowDefinitionId,
                activityId: activityId,
                workflowInstanceId: workflowInstanceId,
                tenantId: tenantId,
                dateTimeOffset);
        }

    }
}
