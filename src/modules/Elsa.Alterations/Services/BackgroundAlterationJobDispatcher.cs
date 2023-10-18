using Elsa.Alterations.Core.Contracts;
using Elsa.Mediator.Contracts;

namespace Elsa.Alterations.Services;

/// <summary>
/// Dispatches an alteration job for execution using an in-memory channel.
/// </summary>
public class BackgroundAlterationJobDispatcher : IAlterationJobDispatcher
{
    private readonly IJobQueue _jobQueue;
    private readonly IAlterationJobRunner _alterationJobRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundAlterationJobDispatcher"/> class.
    /// </summary>
    public BackgroundAlterationJobDispatcher(IJobQueue jobQueue, IAlterationJobRunner alterationJobRunner)
    {
        _jobQueue = jobQueue;
        _alterationJobRunner = alterationJobRunner;
    }
    
    /// <inheritdoc />
    public ValueTask DispatchAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _jobQueue.Enqueue(ct => ExecuteJobAsync(jobId, ct));
        return default;
    }
    
    private async Task ExecuteJobAsync(string alterationJobId, CancellationToken cancellationToken)
    {
        await _alterationJobRunner.RunAsync(alterationJobId, cancellationToken);
    }
}