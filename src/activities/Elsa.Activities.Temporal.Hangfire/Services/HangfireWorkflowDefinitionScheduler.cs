using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Activities.Temporal.Hangfire.Extensions;
using Elsa.Activities.Temporal.Hangfire.Jobs;
using Elsa.Activities.Temporal.Hangfire.Models;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using NodaTime;

namespace Elsa.Activities.Temporal.Hangfire.Services
{
    public class HangfireWorkflowDefinitionScheduler : IWorkflowDefinitionScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly JobStorage _jobStorage;

        public HangfireWorkflowDefinitionScheduler(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager, JobStorage jobStorage)
        {
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _jobStorage = jobStorage;
        }

        public Task ScheduleAsync(string workflowDefinitionId, string activityId, Instant startAt, Duration? interval, CancellationToken cancellationToken = default)
        {
            var cronExpression = interval?.ToCronExpression();
            var data = CreateData(workflowDefinitionId, activityId, cronExpression);

            ScheduleJob(data, startAt);

            if (cronExpression != null)
                ScheduleRecurringJob(data, cronExpression);

            return Task.CompletedTask;
        }

        public Task ScheduleAsync(string workflowDefinitionId, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowDefinitionId, activityId, cronExpression);
            ScheduleRecurringJob(data, cronExpression);
            return Task.CompletedTask;
        }

        public Task UnscheduleAsync(string workflowDefinitionId, string activityId, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowDefinitionId, activityId);
            DeleteRecurringJob(data);
            return Task.CompletedTask;
        }

        public Task UnscheduleAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            DeleteRecurringJobs(workflowDefinitionId);
            return Task.CompletedTask;
        }

        public Task UnscheduleAllAsync(CancellationToken cancellationToken = default)
        {
            DeleteAllRecurringJobs();
            return Task.CompletedTask;
        }

        private void ScheduleJob(RunHangfireWorkflowDefinitionJobModel data, Instant instant) => _backgroundJobClient.Schedule<RunHangfireWorkflowDefinitionJob>(job => job.ExecuteAsync(data), instant.ToDateTimeOffset());

        private void ScheduleRecurringJob(RunHangfireWorkflowDefinitionJobModel data, string cronExpression)
        {
            var identity = data.GetIdentity();
            _recurringJobManager.AddOrUpdate<RunHangfireWorkflowDefinitionJob>(identity, job => job.ExecuteAsync(data), cronExpression);
        }

        private void DeleteRecurringJob(RunHangfireWorkflowDefinitionJobModel data)
        {
            var identity = data.GetIdentity();
            _recurringJobManager.RemoveIfExists(identity);
        }

        private void DeleteRecurringJobs(string workflowDefinitionId)
        {
            var recurringJobs = QueryRecurringJobs();

            var recurringJobsToRemove = recurringJobs.Where(x =>
            {
                var model = GetJobModel(x.Job);
                return model.WorkflowDefinitionId == workflowDefinitionId;
            }).ToList();

            foreach (var job in recurringJobsToRemove)
                _recurringJobManager.RemoveIfExists(job.Id);
        }

        private void DeleteAllRecurringJobs()
        {
            var recurringJobs = QueryRecurringJobs().ToList();

            foreach (var job in recurringJobs)
                _recurringJobManager.RemoveIfExists(job.Id);
        }

        private IEnumerable<RecurringJobDto> QueryRecurringJobs() => _jobStorage.GetConnection().GetRecurringJobs().Where(x => x.Job.Type == typeof(RunHangfireWorkflowDefinitionJob));
        private static RunHangfireWorkflowDefinitionJobModel GetJobModel(Job job) => (RunHangfireWorkflowDefinitionJobModel) job.Args[0];

        private static RunHangfireWorkflowDefinitionJobModel CreateData(string workflowDefinitionId, string activityId, string? cronExpression = default) => new(workflowDefinitionId, activityId, cronExpression);
    }
}