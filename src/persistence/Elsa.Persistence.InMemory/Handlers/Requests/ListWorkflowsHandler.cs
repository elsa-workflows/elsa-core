using System.Collections.Generic;
using System.Linq;
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

public class ListWorkflowsHandler : IRequestHandler<ListWorkflows, IEnumerable<Workflow>>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;
    private readonly WorkflowDefinitionMapper _mapper;

    public ListWorkflowsHandler(InMemoryStore<WorkflowDefinition> store, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _mapper = mapper;
    }

    public Task<IEnumerable<Workflow>> HandleAsync(ListWorkflows request, CancellationToken cancellationToken)
    {
        var query = _store.List();

        if (request.VersionOptions != null)
            query = query.WithVersion(request.VersionOptions.Value);
        
        var entities = query.Skip(request.Skip).Take(request.Take).ToList();
        var workflows = entities.Select(x => _mapper.Map(x)!).ToList().AsEnumerable();
        return Task.FromResult(workflows);
    }
}