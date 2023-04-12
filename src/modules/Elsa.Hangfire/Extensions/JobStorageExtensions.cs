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
    public static IEnumerable<KeyValuePair<string, ScheduledJobDto>> EnumerateScheduledJobs<TJob>(this JobStorage storage, string name)
    {
        var api = storage.GetMonitoringApi();
        var skip = 0;
        const int take = 100;
        JobList<ScheduledJobDto> jobList;

        do
        {
            jobList = api.ScheduledJobs(skip, take);

            var jobs = jobList.FindAll(x => x.Value.Job.Type == typeof(TJob));
            foreach (var job in jobs.Where(x => (string)x.Value.Job.Args[0] == name))
                yield return job;

            skip += take;
        } while (jobList.Count == take);
    }
}