using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Runtimes.DistributedLockingRuntime.Commands;

/// <summary>
/// A handler that executes the <see cref="ExecuteWorkflowCommand"/>.
/// </summary>
public class ExecuteWorkflowCommandHandler(IWorkflowClient workflowClient) : ICommandHandler<ExecuteWorkflowCommand>
{
    /// <inheritdoc />
    public async Task<Unit> HandleAsync(ExecuteWorkflowCommand command, CancellationToken cancellationToken)
    {
        await workflowClient.ExecuteAndWaitAsync(command.ExecuteWorkflowParams, cancellationToken);
        return Unit.Instance;
    }
}