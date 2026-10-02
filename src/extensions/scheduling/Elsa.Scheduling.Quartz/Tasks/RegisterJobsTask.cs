using Elsa.Common;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Quartz;
using QuartzIScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.Tasks;

/// <summary>
/// Registers the Quartz jobs.
/// </summary>
/// <param name="schedulerFactoryFactory"></param>
/// <param name="jobKeyProvider"></param>
/// <param name="logger"></param>
[UsedImplicitly]
internal class RegisterJobsTask(ISchedulerFactory schedulerFactoryFactory, IJobKeyProvider jobKeyProvider, ILogger<RegisterJobsTask> logger) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        await CreateJobAsync<RunWorkflowJob>(scheduler, cancellationToken);
        await CreateJobAsync<ResumeWorkflowJob>(scheduler, cancellationToken);
    }
    
    private async Task CreateJobAsync<TJobType>(QuartzIScheduler scheduler, CancellationToken cancellationToken) where TJobType : IJob
    {
        var key = jobKeyProvider.GetJobKey<TJobType>();
        var job = JobBuilder.Create<TJobType>()
            .WithIdentity(key)
            .StoreDurably()
            .Build();

        try
        {
            // Try to add the job. In clustered mode, multiple instances may attempt this simultaneously.
            // Use replace=false to ensure we don't overwrite an existing job definition.
            const bool replaceExisting = false;
            await scheduler.AddJob(job, replaceExisting, cancellationToken);
        }
        catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
        {
            // Job already exists, which is fine in clustered scenarios where multiple pods
            // may start concurrently. This is an expected race condition and can be safely ignored.
            logger.LogDebug("Job {JobKey} already exists, skipping registration. This is expected in clustered deployments during concurrent startup", key);
        }
        catch (ObjectAlreadyExistsException)
        {
            // Job already exists, which is fine in clustered scenarios where multiple pods
            // may start concurrently. This is an expected race condition and can be safely ignored.
            logger.LogDebug("Job {JobKey} already exists, skipping registration. This is expected in clustered deployments during concurrent startup", key);
        }
    }
}