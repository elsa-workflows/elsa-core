using Elsa.Modules.Quartz.Contracts;
using Elsa.Modules.Quartz.Services;
using Elsa.Scheduling.Contracts;
using Quartz;
using IElsaJob = Elsa.Scheduling.Contracts.IJob;
using IQuartzJob = Quartz.IJob;

namespace Elsa.Modules.Quartz.Jobs;

/// <summary>
/// A generic Quartz job that executes Elsa scheduled jobs.
/// </summary>
/// <typeparam name="TElsaJob"></typeparam>
public class QuartzJob<TElsaJob> : IQuartzJob where TElsaJob : IElsaJob
{
    private readonly IElsaJobSerializer _elsaJobSerializer;
    private readonly IJobManager _jobManager;

    public QuartzJob(IElsaJobSerializer elsaJobSerializer, IJobManager jobManager)
    {
        _elsaJobSerializer = elsaJobSerializer;
        _jobManager = jobManager;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var json = context.MergedJobDataMap.GetString(QuartzJobScheduler.JobDataKey)!;
        var elsaJob = _elsaJobSerializer.Deserialize<TElsaJob>(json);
        await _jobManager.ExecuteJobAsync(elsaJob, context.CancellationToken);
    }
}