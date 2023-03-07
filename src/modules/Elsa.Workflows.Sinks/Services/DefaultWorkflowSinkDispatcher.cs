using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Sinks.Commands;
using Elsa.Workflows.Sinks.Contracts;

namespace Elsa.Workflows.Sinks.Services;

/// <summary>
/// A default <see cref="IWorkflowSinkDispatcher"/> implementation that uses the <see cref="IBackgroundCommandSender"/> service for asynchronous, in-process invocation of workflow state sinks.
/// </summary>
public class DefaultWorkflowSinkDispatcher : IWorkflowSinkDispatcher
{
    private readonly IBackgroundCommandSender _sender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowSinkDispatcher(IBackgroundCommandSender sender)
    {
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var command = new ProcessWorkflowState(workflowState);
        await _sender.SendAsync(command, cancellationToken);
    }
}