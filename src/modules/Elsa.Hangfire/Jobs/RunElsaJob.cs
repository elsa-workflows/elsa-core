using Elsa.Jobs.Contracts;

namespace Elsa.Hangfire.Jobs;

/// <summary>
/// A generic Hangfire job that executes the specified Elsa job. Basically a proxy.
/// </summary>
public class RunElsaJob
{
    private readonly IJobRunner _jobRunner;

    public RunElsaJob(IJobRunner jobRunner)
    {
        _jobRunner = jobRunner;
    }
    
    public async Task RunAsync(IJob job, CancellationToken cancellationToken)
    {
        await _jobRunner.RunJobAsync(job, cancellationToken);
    }
}