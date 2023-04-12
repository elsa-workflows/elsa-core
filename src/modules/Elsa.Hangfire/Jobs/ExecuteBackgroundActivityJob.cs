using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Hangfire.Jobs;

/// <summary>
/// A job that executes a background activity.
/// </summary>
public class ExecuteBackgroundActivityJob
{
    private readonly IBackgroundActivityInvoker _backgroundActivityInvoker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteBackgroundActivityJob"/> class.
    /// </summary>
    /// <param name="backgroundActivityInvoker"></param>
    public ExecuteBackgroundActivityJob(IBackgroundActivityInvoker backgroundActivityInvoker)
    {
        _backgroundActivityInvoker = backgroundActivityInvoker;
    }
    
    /// <summary>
    /// Executes the job.
    /// </summary>
    public async Task ExecuteAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        await _backgroundActivityInvoker.ExecuteAsync(scheduledBackgroundActivity, cancellationToken);
    }
}