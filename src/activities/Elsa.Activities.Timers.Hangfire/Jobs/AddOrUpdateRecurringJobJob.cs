using Elsa.Activities.Timers.Hangfire.Models;

using Hangfire;

namespace Elsa.Activities.Timers.Hangfire.Jobs
{
    public class AddOrUpdateRecurringJobJob
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public AddOrUpdateRecurringJobJob(IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public void Execute(RunHangfireWorkflowJobModel data, string cronExpression)
        {
            RecurringJob.AddOrUpdate<RunHangfireWorkflowJob>(data.GetIdentity(), job => job.ExecuteAsync(data), cronExpression);
        }
    }
}
