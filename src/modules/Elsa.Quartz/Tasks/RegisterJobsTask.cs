using Elsa.Common;
using Elsa.Quartz.Contracts;
using Elsa.Quartz.Jobs;
using JetBrains.Annotations;
using Quartz;

namespace Elsa.Quartz.Tasks;

/// <summary>
/// Registers the Quartz jobs.
/// </summary>
/// <param name="schedulerFactoryFactory"></param>
/// <param name="jobKeyProvider"></param>
[UsedImplicitly]
internal class RegisterJobsTask(ISchedulerFactory schedulerFactoryFactory, IJobKeyProvider jobKeyProvider) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        await CreateJobAsync<RunWorkflowJob>(scheduler, cancellationToken);
        await CreateJobAsync<ResumeWorkflowJob>(scheduler, cancellationToken);
    }
    
    private async Task CreateJobAsync<TJobType>(IScheduler scheduler, CancellationToken cancellationToken) where TJobType : IJob
    {
        var key = jobKeyProvider.GetJobKey<TJobType>();
        var job = JobBuilder.Create<TJobType>()
            .WithIdentity(key)
            .StoreDurably()
            .Build();
        
        if (!await scheduler.CheckExists(job.Key, cancellationToken))
            await scheduler.AddJob(job, false, cancellationToken);
    }
}