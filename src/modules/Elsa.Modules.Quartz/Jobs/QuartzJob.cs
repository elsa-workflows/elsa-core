using Elsa.Jobs.Contracts;
using Elsa.Modules.Quartz.Contracts;
using Elsa.Modules.Quartz.Services;
using Quartz;
using IJob = Elsa.Jobs.Contracts.IJob;
using IQuartzJob = Quartz.IJob;

namespace Elsa.Modules.Quartz.Jobs;

/// <summary>
/// A generic Quartz job that executes Elsa scheduled jobs.
/// </summary>
/// <typeparam name="TElsaJob"></typeparam>
public class QuartzJob<TElsaJob> : IQuartzJob where TElsaJob : IJob
{
    private readonly IElsaJobSerializer _elsaJobSerializer;
    private readonly IJobRunner _jobRunner;

    public QuartzJob(IElsaJobSerializer elsaJobSerializer, IJobRunner jobRunner)
    {
        _elsaJobSerializer = elsaJobSerializer;
        _jobRunner = jobRunner;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var json = context.MergedJobDataMap.GetString(QuartzJobScheduler.JobDataKey)!;
        var elsaJob = _elsaJobSerializer.Deserialize<TElsaJob>(json);
        await _jobRunner.RunJobAsync(elsaJob, context.CancellationToken);
    }
}