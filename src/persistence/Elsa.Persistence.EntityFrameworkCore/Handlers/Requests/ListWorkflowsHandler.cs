using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Mappers;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class ListWorkflowsHandler : IRequestHandler<ListWorkflows, IEnumerable<Workflow>>
{
    private readonly IStore<WorkflowDefinition> _store;
    private readonly WorkflowDefinitionMapper _mapper;

    public ListWorkflowsHandler(IStore<WorkflowDefinition> store, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Workflow>> HandleAsync(ListWorkflows request, CancellationToken cancellationToken)
    {
        var definitions = await _store.QueryAsync(query =>
        {
            if (request.VersionOptions != null)
                query = query.WithVersion(request.VersionOptions.Value);

            return query.Skip(request.Skip).Take(request.Take);
        }, cancellationToken);

        return definitions.Select(x => _mapper.Map(x)!).ToList();
    }
}