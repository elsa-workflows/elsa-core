using Elsa.Hangfire.Jobs;
using Hangfire;
using Hangfire.Storage.Monitoring;

namespace Elsa.Hangfire.Extensions;

/// <summary>
/// A set of extension methods for <see cref="JobStorage"/>.
/// </summary>
public static class JobStorageExtensions
{
    /// <summary>
    /// Enumerates all scheduled jobs of a given type.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, ScheduledJobDto>> EnumerateScheduledJobs(this JobStorage storage, string name)
    {
        var api = storage.GetMonitoringApi();
        var skip = 0;
        const int take = 100;
        JobList<ScheduledJobDto> scheduledJobs;

        do
        {
            scheduledJobs = api.ScheduledJobs(skip, take);

            var jobs = scheduledJobs.FindAll(x => x.Value.Job.Type == typeof(RunWorkflowJob) || x.Value.Job.Type == typeof(ResumeWorkflowJob));
            foreach (var job in jobs.Where(x => (string)x.Value.Job.Args[0] == name))
                yield return job;

            skip += take;
        } while (scheduledJobs.Count == take);
    }

    /// <summary>
    /// Enumerates all enqueued jobs of a given type.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, EnqueuedJobDto>> EnumerateQueuedJobs(this JobStorage storage, string queueName, string taskName)
    {
        var api = storage.GetMonitoringApi();
        var skip = 0;
        const int take = 100;
        JobList<EnqueuedJobDto> enqueuedJobs;

        do
        {
            enqueuedJobs = api.EnqueuedJobs(queueName, skip, take);

            var jobs = enqueuedJobs.FindAll(x => x.Value.Job.Type == typeof(RunWorkflowJob) || x.Value.Job.Type == typeof(ResumeWorkflowJob));
            foreach (var job in jobs.Where(x => (string)x.Value.Job.Args[0] == taskName))
                yield return job;

            skip += take;
        } while (enqueuedJobs.Count == take);
    }
}