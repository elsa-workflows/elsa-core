using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Executes activities that are temporarily detached from the workflow. This allows activities to execute in the background, for example.
/// </summary>
public interface IBackgroundActivityInvoker
{
    /// <summary>
    /// Executes the specified activity detached from the workflow.
    /// </summary>
    /// <param name="scheduledBackgroundActivity">The scheduled background activity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A handle representing the asynchronous invocation.</returns>
    Task ExecuteAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default);
}