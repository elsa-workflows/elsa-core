using Elsa.Jobs.Schedules;
using Elsa.Jobs.Services;
using Elsa.Scheduling.Jobs;
using Elsa.Scheduling.Services;

namespace Elsa.Scheduling.Implementations;

public class ElasticCongurationScheduler : IElasticCongurationScheduler
{
    private readonly IJobScheduler _jobScheduler;

    public ElasticCongurationScheduler(IJobScheduler jobScheduler)
    {
        _jobScheduler = jobScheduler;
    }

    public async Task ScheduleAsync(CancellationToken cancellationToken = default)
    {
        var job = new ConfigureElasticIndicesJob();
        var schedule = new CronSchedule
        {
            //Last day of every month
            CronExpression = "0 18 L * ?"
        };
        
        await _jobScheduler.ScheduleAsync(job, GetType().Name, schedule, cancellationToken: cancellationToken);
    }
}