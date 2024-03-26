using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Handles the <see cref="CancelWorkflowsCommand"/>.
/// </summary>
public class CancelWorkflowsCommandHandler(IWorkflowRuntime workflowRuntime) : ICommandHandler<CancelWorkflowsCommand>
{
    /// <inheritdoc />
    public async Task<Unit> HandleAsync(CancelWorkflowsCommand command, CancellationToken cancellationToken)
    {
        await workflowRuntime.CancelWorkflowAsync(command.Request.WorkflowInstanceId, cancellationToken);
        
        return Unit.Instance;
    }
}