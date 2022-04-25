using System.Linq.Expressions;
using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Mappers;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowHandler : IRequestHandler<FindWorkflowByDefinitionId, Workflow?>, IRequestHandler<FindWorkflowByName, Workflow?>
{
    private readonly IStore<WorkflowDefinition> _store;
    private readonly WorkflowDefinitionMapper _mapper;

    public FindWorkflowHandler(IStore<WorkflowDefinition> store, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _mapper = mapper;
    }

    public async Task<Workflow?> HandleAsync(FindWorkflowByDefinitionId request, CancellationToken cancellationToken)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == request.DefinitionId;
        predicate = predicate.WithVersion(request.VersionOptions);
        var definition = await _store.FindAsync(predicate, cancellationToken);

        return _mapper.Map(definition);
    }

    public async Task<Workflow?> HandleAsync(FindWorkflowByName request, CancellationToken cancellationToken)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.Name == request.Name;
        predicate = predicate.WithVersion(request.VersionOptions);
        var definition = await _store.FindAsync(predicate, cancellationToken);

        return _mapper.Map(definition);
    }
}