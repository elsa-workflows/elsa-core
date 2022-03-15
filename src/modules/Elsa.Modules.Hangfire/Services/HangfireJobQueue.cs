using Elsa.Jobs.Contracts;
using Hangfire;
using Hangfire.States;
using HangfireJob = Hangfire.Common.Job;

namespace Elsa.Modules.Hangfire.Services;

public class HangfireJobQueue : IJobQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IJobRunner _jobRunner;

    public HangfireJobQueue(IBackgroundJobClient backgroundJobClient, IJobRunner jobRunner)
    {
        _backgroundJobClient = backgroundJobClient;
        _jobRunner = jobRunner;
    }

    public Task SubmitJobAsync(IJob job, string? queueName = default, CancellationToken cancellationToken = default)
    {
        var hangfireJob = HangfireJob.FromExpression<IJob>(x => _jobRunner.RunJobAsync(x, CancellationToken.None));
        _backgroundJobClient.Create(hangfireJob, new EnqueuedState(queueName ?? "default"));
        
        return Task.CompletedTask;
    }
}