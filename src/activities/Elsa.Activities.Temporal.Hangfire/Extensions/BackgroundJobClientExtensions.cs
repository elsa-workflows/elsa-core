using System;
using System.Linq;
using Elsa.Activities.Temporal.Hangfire.Jobs;
using Elsa.Activities.Temporal.Hangfire.Models;
using Hangfire;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
{
    public static class BackgroundJobClientExtensions
    {
        public static void ScheduleWorkflow(this IBackgroundJobClient backgroundJobClient, RunHangfireWorkflowJobModel data, DateTimeOffset dateTimeOffset)
        {
            backgroundJobClient.UnscheduleJobWhenAlreadyExists(data);
            backgroundJobClient.Schedule<RunHangfireWorkflowJob>(job => job.ExecuteAsync(data), dateTimeOffset);
        }

        public static void UnscheduleJobWhenAlreadyExists(this IBackgroundJobClient backgroundJobClient, RunHangfireWorkflowJobModel data)
        {
            var identity = data.GetIdentity();
            var monitor = JobStorage.Current.GetMonitoringApi();
            var workflowJobType = typeof(RunHangfireWorkflowJob);

            var jobs = monitor.ScheduledJobs(0, int.MaxValue)
               .Where(x => x.Value.Job.Type == workflowJobType && ((RunHangfireWorkflowJobModel)x.Value.Job.Args[0]).GetIdentity() == identity);

            foreach (var job in jobs) 
                backgroundJobClient.Delete(job.Key);
        }
        
        public static void UnscheduleJobWhenAlreadyExists(this IBackgroundJobClient backgroundJobClient, string workflowDefinitionId, string? tenantId)
        {
            var monitor = JobStorage.Current.GetMonitoringApi();
            var workflowJobType = typeof(RunHangfireWorkflowJob);

            var jobs = monitor.ScheduledJobs(0, int.MaxValue)
                .Where(x =>
                {
                    var model = (RunHangfireWorkflowJobModel) x.Value.Job.Args[0];
                    return x.Value.Job.Type == workflowJobType && model.WorkflowDefinitionId == workflowDefinitionId && model.TenantId == tenantId;
                });

            foreach (var job in jobs) 
                backgroundJobClient.Delete(job.Key);
        }
    }
}
