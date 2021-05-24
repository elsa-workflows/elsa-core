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
    public class HangfireWorkflowInstanceScheduler : IWorkflowInstanceScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly JobStorage _jobStorage;

        public HangfireWorkflowInstanceScheduler(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager, JobStorage jobStorage)
        {
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _jobStorage = jobStorage;
        }
        
        public Task ScheduleAsync(string workflowInstanceId, string activityId, Instant startAt, Duration? interval, CancellationToken cancellationToken = default)
        {
            var cronExpression = interval?.ToCronExpression();
            var data = CreateData(workflowInstanceId, activityId, cronExpression);

            ScheduleJob(data, startAt);

            if (cronExpression != null)
                ScheduleRecurringJob(data, cronExpression);

            return Task.CompletedTask;
        }

        public Task ScheduleAsync(string workflowInstanceId, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowInstanceId, activityId, cronExpression);
            ScheduleRecurringJob(data, cronExpression);
            return Task.CompletedTask;
        }

        public Task UnscheduleAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default)
        {
            var data = CreateData(workflowInstanceId, activityId);
            DeleteRecurringJob(data);
            return Task.CompletedTask;
        }

        public Task UnscheduleAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            DeleteRecurringJobs(workflowInstanceId);
            return Task.CompletedTask;
        }

        public Task UnscheduleAllAsync(CancellationToken cancellationToken = default)
        {
            DeleteAllRecurringJobs();
            return Task.CompletedTask;
        }

        private void ScheduleJob(RunHangfireWorkflowInstanceJobModel data, Instant instant) => _backgroundJobClient.Schedule<RunHangfireWorkflowInstanceJob>(job => job.ExecuteAsync(data), instant.ToDateTimeOffset());

        private void ScheduleRecurringJob(RunHangfireWorkflowInstanceJobModel data, string cronExpression)
        {
            var identity = data.GetIdentity();
            _recurringJobManager.AddOrUpdate<RunHangfireWorkflowInstanceJob>(identity, job => job.ExecuteAsync(data), cronExpression);
        }

        private void DeleteRecurringJob(RunHangfireWorkflowInstanceJobModel data)
        {
            var identity = data.GetIdentity();
            _recurringJobManager.RemoveIfExists(identity);
        }
        
        private void DeleteRecurringJobs(string workflowInstanceId)
        {
            var recurringJobs = QueryRecurringJobs();

            var recurringJobsToRemove = recurringJobs.Where(x =>
            {
                var model = GetJobModel(x.Job);
                return model.WorkflowInstanceId == workflowInstanceId;
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

        private IEnumerable<RecurringJobDto> QueryRecurringJobs() => _jobStorage.GetConnection().GetRecurringJobs().Where(x => x.Job.Type == typeof(RunHangfireWorkflowInstanceJob));
        private static RunHangfireWorkflowInstanceJobModel GetJobModel(Job job) => (RunHangfireWorkflowInstanceJobModel) job.Args[0];
        private static RunHangfireWorkflowInstanceJobModel CreateData(string workflowInstanceId, string activityId, string? cronExpression = default) => new(workflowInstanceId, activityId, cronExpression);
    }
}