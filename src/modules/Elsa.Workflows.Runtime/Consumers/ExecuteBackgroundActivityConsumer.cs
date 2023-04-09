using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Consumers;

/// <summary>
/// A consumer that executes an activity in the background.
/// </summary>
public class ExecuteBackgroundActivityConsumer : IConsumer<ScheduledBackgroundActivity>
{
    private readonly IBackgroundActivityInvoker _backgroundActivityInvoker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteBackgroundActivityConsumer"/> class.
    /// </summary>
    public ExecuteBackgroundActivityConsumer(IBackgroundActivityInvoker backgroundActivityInvoker, IWorkflowDispatcher workflowDispatcher)
    {
        _backgroundActivityInvoker = backgroundActivityInvoker;
    }

    /// <inheritdoc />
    public async ValueTask ConsumeAsync(ScheduledBackgroundActivity message, CancellationToken cancellationToken)
    {
        // Execute the activity.
        await _backgroundActivityInvoker.ExecuteAsync(message, cancellationToken);
    }
}