using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Management.Commands;

namespace Elsa.Workflows.Management.Handlers.Commands;

public class SaveWorkflowDefinitionHandler(IWorkflowDefinitionStore store) : ICommandHandler<SaveWorkflowDefinitionCommand>
{
    public async Task<Unit> HandleAsync(SaveWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        await store.SaveAsync(command.WorkflowDefinition, cancellationToken);
        return Unit.Instance;
    }
}