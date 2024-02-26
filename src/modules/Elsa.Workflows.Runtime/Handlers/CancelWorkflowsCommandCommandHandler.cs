using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Handles the <see cref="DispatchWorkflowDefinitionCommand"/>.
/// </summary>
public class CancelWorkflowsCommandHandler(IWorkflowRuntime workflowRuntime) : ICommandHandler<CancelWorkflowsCommand>
{
    public async Task<Unit> HandleAsync(CancelWorkflowsCommand command, CancellationToken cancellationToken)
    {
        // TODO: Implement cancellation.
        return Unit.Instance;
    }
}