using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Hangfire.Storage.Monitoring;

namespace Elsa.Activities.Temporal.Hangfire.Extensions
{
    public static class JobStorageExtensions
    {
        public static IEnumerable<KeyValuePair<string, ScheduledJobDto>> EnumerateScheduledJobs<TJob, TJobModel>(this JobStorage storage, Func<TJobModel, bool> predicate)
        {
            var api = storage.GetMonitoringApi();
            var skip = 0;
            const int take = 100;
            JobList<ScheduledJobDto> jobList;

            do
            {
                jobList = api.ScheduledJobs(skip, take);

                var jobs = jobList.FindAll(x => x.Value?.Job?.Type == typeof(TJob));
                foreach (var job in jobs)
                {
                    var args = job.Value?.Job?.Args;
                    
                    if (args is not { Count: 1 })
                        continue;

                    if(args[0] is not TJobModel jobModel)
                        continue;
                    
                    if (predicate(jobModel)) 
                        yield return job;
                }

                skip += take;
            } while (jobList.Count == take);
        }
    }
}