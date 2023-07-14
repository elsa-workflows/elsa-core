using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Sinks.Commands;
using Elsa.Workflows.Sinks.Contracts;

namespace Elsa.Workflows.Sinks.Services;

/// <summary>
/// Uses the <see cref="ICommandSender"/> to process the received workflow state from a background worker.
/// </summary>
public class BackgroundWorkflowSinkDispatcher : IWorkflowSinkDispatcher
{
    private readonly ICommandSender _sender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public BackgroundWorkflowSinkDispatcher(ICommandSender sender)
    {
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var command = new ProcessWorkflowState(workflowState);
        await _sender.SendAsync(command, CommandStrategy.Background, cancellationToken);
    }
}