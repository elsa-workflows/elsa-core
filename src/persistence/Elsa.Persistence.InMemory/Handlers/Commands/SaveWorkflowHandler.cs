using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Mappers;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class SaveWorkflowHandler : ICommandHandler<SaveWorkflow>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;
    private readonly WorkflowDefinitionMapper _mapper;

    public SaveWorkflowHandler(InMemoryStore<WorkflowDefinition> store, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _mapper = mapper;
    }

    public Task<Unit> HandleAsync(SaveWorkflow command, CancellationToken cancellationToken)
    {
        var definition = _mapper.Map(command.Workflow)!;
        _store.Save(definition);
        
        return Task.FromResult(Unit.Instance);
    }
}