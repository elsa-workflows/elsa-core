using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Temporal.Hangfire.Jobs;
using Elsa.Activities.Temporal.Hangfire.Models;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using NodaTime;

namespace Elsa.Activities.Temporal.Hangfire.Services
{
    public class JobManager
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly JobStorage _jobStorage;

        public JobManager(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager, JobStorage jobStorage)
        {
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _jobStorage = jobStorage;
        }

        public void ScheduleJob(RunHangfireWorkflowJobModel data, Instant instant)
        {
            UnscheduleJob(data);
            _backgroundJobClient.Schedule<RunHangfireWorkflowJob>(job => job.ExecuteAsync(data), instant.ToDateTimeOffset());
        }

        public void ScheduleJob(RunHangfireWorkflowJobModel data, string cronExpression)
        {
            var identity = data.GetIdentity();
            _recurringJobManager.AddOrUpdate<RunHangfireWorkflowJob>(identity, job => job.ExecuteAsync(data), cronExpression);
        }

        public void UnscheduleJob(RunHangfireWorkflowJobModel data)
        {
            var identity = data.GetIdentity();
            UnscheduleJob(identity);
        }

        public void UnscheduleJob(string identity)
        {
            var job = QueryJobs().FirstOrDefault(x => GetJobModel(x.Job).GetIdentity() == identity);

            if (job == null)
                return;

            _backgroundJobClient.Delete(job.Id);
            _recurringJobManager.RemoveIfExists(job.Id);
        }

        public void UnscheduleJobs(string workflowDefinitionId, string? tenantId)
        {
            var jobs = QueryJobs();

            var jobsToRemove = jobs.Where(x =>
            {
                var model = GetJobModel(x.Job);
                return model.WorkflowDefinitionId == workflowDefinitionId && model.TenantId == tenantId;
            }).ToList();

            foreach (var job in jobsToRemove)
                _recurringJobManager.RemoveIfExists(job.Id);
        }

        private RunHangfireWorkflowJobModel GetJobModel(Job job) => (RunHangfireWorkflowJobModel) job.Args[0];
        private IEnumerable<RecurringJobDto> QueryJobs() => _jobStorage.GetConnection().GetRecurringJobs().Where(x => x.Job.Type == typeof(RunHangfireWorkflowJob));
    }
}