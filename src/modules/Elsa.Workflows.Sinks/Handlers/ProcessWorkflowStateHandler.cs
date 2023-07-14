using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Sinks.Commands;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Services;

namespace Elsa.Workflows.Sinks.Handlers;

/// <summary>
/// A handler for the <see cref="ProcessWorkflowState"/> command.
/// Only used when the <see cref="BackgroundWorkflowSinkDispatcher"/> is used, which internally relies on the mediator.
/// </summary>
internal class ProcessWorkflowStateHandler : ICommandHandler<ProcessWorkflowState>
{
    private readonly IWorkflowSinkInvoker _workflowSinkInvoker;

    public ProcessWorkflowStateHandler(IWorkflowSinkInvoker workflowSinkInvoker)
    {
        _workflowSinkInvoker = workflowSinkInvoker;
    }
    
    /// <summary>
    /// Invokes all registered workflow sinks using the workflow sink invoker.
    /// </summary>
    public async Task<Unit> HandleAsync(ProcessWorkflowState command, CancellationToken cancellationToken)
    {
        await _workflowSinkInvoker.InvokeAsync(command.WorkflowState, cancellationToken);
        return Unit.Instance;
    }
}