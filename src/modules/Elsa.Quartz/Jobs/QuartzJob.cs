using Elsa.Jobs.Contracts;
using Elsa.Quartz.Services;
using Quartz;
using IJob = Elsa.Jobs.Contracts.IJob;
using IQuartzJob = Quartz.IJob;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A generic Quartz job that executes Elsa scheduled jobs.
/// </summary>
/// <typeparam name="TElsaJob"></typeparam>
public class QuartzJob<TElsaJob> : IQuartzJob where TElsaJob : IJob
{
    private readonly IJobSerializer _jobSerializer;
    private readonly IJobRunner _jobRunner;

    public QuartzJob(IJobSerializer jobSerializer, IJobRunner jobRunner)
    {
        _jobSerializer = jobSerializer;
        _jobRunner = jobRunner;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var json = context.MergedJobDataMap.GetString(QuartzJobScheduler.JobDataKey)!;
        var elsaJob = _jobSerializer.Deserialize<TElsaJob>(json);
        await _jobRunner.RunJobAsync(elsaJob, context.CancellationToken);
    }
}