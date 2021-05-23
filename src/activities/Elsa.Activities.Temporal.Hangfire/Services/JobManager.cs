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
            _backgroundJobClient.Schedule<RunHangfireWorkflowJob>(job => job.ExecuteAsync(data), instant.ToDateTimeOffset());
        }

        public void ScheduleRecurringJob(RunHangfireWorkflowJobModel data, string cronExpression)
        {
            var identity = data.GetIdentity();
            _recurringJobManager.AddOrUpdate<RunHangfireWorkflowJob>(identity, job => job.ExecuteAsync(data), cronExpression);
        }

        public void DeleteJob(RunHangfireWorkflowJobModel data)
        {
            DeleteRecurringJob(data);
        }

        public void DeleteRecurringJob(RunHangfireWorkflowJobModel data)
        {
            var identity = data.GetIdentity();
            _recurringJobManager.RemoveIfExists(identity);
        }

        public void DeleteJobs(string workflowDefinitionId, string? tenantId)
        {
            DeleteRecurringJobs(workflowDefinitionId, tenantId);
        }

        private void DeleteRecurringJobs(string workflowDefinitionId, string? tenantId)
        {
            var recurringJobs = QueryRecurringJobs();

            var recurringJobsToRemove = recurringJobs.Where(x =>
            {
                var model = GetJobModel(x.Job);
                return model.WorkflowDefinitionId == workflowDefinitionId && model.TenantId == tenantId;
            }).ToList();

            foreach (var job in recurringJobsToRemove)
                _recurringJobManager.RemoveIfExists(job.Id);
        }

        private RunHangfireWorkflowJobModel GetJobModel(Job job) => (RunHangfireWorkflowJobModel) job.Args[0];
        private IEnumerable<RecurringJobDto> QueryRecurringJobs() => _jobStorage.GetConnection().GetRecurringJobs().Where(x => x.Job.Type == typeof(RunHangfireWorkflowJob));
    }

    public class JobPayload
    {
        public IDictionary<string, string> Jobs { get; set; } = new Dictionary<string, string>();
    }
}