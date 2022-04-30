using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;

// ReSharper disable PossibleMultipleEnumeration

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindManyWorkflowDefinitionsHandler : IRequestHandler<FindManyWorkflowDefinitions, IEnumerable<WorkflowDefinitionSummary>>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;
    public FindManyWorkflowDefinitionsHandler(InMemoryStore<WorkflowDefinition> store) => _store = store;

    public Task<IEnumerable<WorkflowDefinitionSummary>> HandleAsync(FindManyWorkflowDefinitions request, CancellationToken cancellationToken)
    {
        var query = _store.List();

        if (request.VersionOptions != null)
            query = query.WithVersion(request.VersionOptions.Value);

        if (request.DefinitionIds != null) 
            query = query.Where(x => request.DefinitionIds.Contains(x.Id));
        
        var summaries = query.Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(summaries);
    }
}