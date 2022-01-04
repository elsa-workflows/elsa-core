using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.Mappers;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class SaveWorkflowDefinitionHandler : ICommandHandler<SaveWorkflow>
{
    private readonly IStore<WorkflowDefinition> _store;
    private readonly WorkflowDefinitionMapper _mapper;

    public SaveWorkflowDefinitionHandler(IStore<WorkflowDefinition> store, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _mapper = mapper;
    }

    public async Task<Unit> HandleAsync(SaveWorkflow command, CancellationToken cancellationToken)
    {
        var definition = _mapper.Map(command.Workflow)!;
        await _store.SaveAsync(definition, cancellationToken);
        
        return Unit.Instance;
    }
}