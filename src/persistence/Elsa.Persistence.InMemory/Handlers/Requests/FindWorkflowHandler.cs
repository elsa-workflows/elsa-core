using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindWorkflowHandler : IRequestHandler<FindWorkflowByDefinitionId, WorkflowDefinition?>, IRequestHandler<FindWorkflowByName, WorkflowDefinition?>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;

    public FindWorkflowHandler(InMemoryStore<WorkflowDefinition> store)
    {
        _store = store;
    }

    public Task<WorkflowDefinition?> HandleAsync(FindWorkflowByDefinitionId request, CancellationToken cancellationToken)
    {
        var definition = _store.Find(x => x.DefinitionId == request.DefinitionId && x.WithVersion(request.VersionOptions));
        return Task.FromResult(definition);
    }

    public Task<WorkflowDefinition?> HandleAsync(FindWorkflowByName request, CancellationToken cancellationToken)
    {
        var definition = _store.Find(x => x.Name == request.Name && x.WithVersion(request.VersionOptions));
        return Task.FromResult(definition);
    }
}