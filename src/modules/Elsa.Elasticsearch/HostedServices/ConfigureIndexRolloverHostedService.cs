using Elsa.Elasticsearch.Scheduling;
using Elsa.Jobs.Schedules;
using Elsa.Jobs.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.Elasticsearch.HostedServices;

public class ConfigureIndexRolloverHostedService : IHostedService
{
    private readonly IJobScheduler _jobScheduler;

    public ConfigureIndexRolloverHostedService(IJobScheduler jobScheduler)
    {
        _jobScheduler = jobScheduler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var job = new ConfigureIndexRolloverJob();
        var schedule = new CronSchedule
        {
            // At the beginning of every month
            CronExpression = "0 0 1 * *"
        };
        
        await _jobScheduler.ScheduleAsync(job, GetType().Name, schedule, cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}