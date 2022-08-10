using Elsa.Hangfire.Jobs;
using Elsa.Jobs.Services;
using Hangfire;
using Hangfire.Server;
using Hangfire.States;
using HangfireJob = Hangfire.Common.Job;

namespace Elsa.Hangfire.Implementations;

public class HangfireJobQueue : IJobQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireJobQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task<string> SubmitJobAsync(IJob job, string? queueName = default, CancellationToken cancellationToken = default)
    {
        var hangfireJob = HangfireJob.FromExpression<RunElsaJob>(x => x.RunAsync(job, CancellationToken.None));
        var jobId = _backgroundJobClient.Create(hangfireJob, new EnqueuedState(queueName ?? "default"));

        return Task.FromResult(jobId);
    }
}