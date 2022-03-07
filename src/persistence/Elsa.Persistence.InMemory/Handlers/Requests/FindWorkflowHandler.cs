using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.InMemory.Services;
using Elsa.Persistence.Mappers;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindWorkflowHandler : IRequestHandler<FindWorkflowByDefinitionId, Workflow?>, IRequestHandler<FindWorkflowByName, Workflow?>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;
    private readonly WorkflowDefinitionMapper _mapper;

    public FindWorkflowHandler(InMemoryStore<WorkflowDefinition> store, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _mapper = mapper;
    }

    public Task<Workflow?> HandleAsync(FindWorkflowByDefinitionId request, CancellationToken cancellationToken)
    {
        var definition = _store.Find(x => x.DefinitionId == request.DefinitionId && x.WithVersion(request.VersionOptions));
        var workflow = _mapper.Map(definition);
        return Task.FromResult(workflow);
    }

    public Task<Workflow?> HandleAsync(FindWorkflowByName request, CancellationToken cancellationToken)
    {
        var definition = _store.Find(x => x.Name == request.Name && x.WithVersion(request.VersionOptions));
        var workflow = _mapper.Map(definition);
        return Task.FromResult(workflow);
    }
}