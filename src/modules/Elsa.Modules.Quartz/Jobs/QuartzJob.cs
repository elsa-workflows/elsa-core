using Elsa.Modules.Quartz.Contracts;
using Elsa.Modules.Quartz.Services;
using Quartz;
using IElsaJob = Elsa.Activities.Scheduling.Contracts.IJob;

namespace Elsa.Modules.Quartz.Jobs;

/// <summary>
/// A generic Quartz job that executes Elsa scheduled jobs.
/// </summary>
/// <typeparam name="TElsaJob"></typeparam>
public class QuartzJob<TElsaJob> : IJob where TElsaJob : IElsaJob
{
    private readonly IElsaJobSerializer _elsaJobSerializer;

    public QuartzJob(IElsaJobSerializer elsaJobSerializer) => _elsaJobSerializer = elsaJobSerializer;

    public async Task Execute(IJobExecutionContext context)
    {
        var json = context.MergedJobDataMap.GetString(QuartzJobScheduler.JobDataKey)!;
        var elsaJob = _elsaJobSerializer.Deserialize<TElsaJob>(json);
        
        await elsaJob.ExecuteAsync(context.CancellationToken);
    }
}