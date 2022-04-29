using System.Linq.Expressions;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowHandler : IRequestHandler<FindWorkflowDefinitionByDefinitionId, WorkflowDefinition?>, IRequestHandler<FindWorkflowDefinitionByName, WorkflowDefinition?>
{
    private readonly IStore<WorkflowDefinition> _store;

    public FindWorkflowHandler(IStore<WorkflowDefinition> store)
    {
        _store = store;
    }

    public async Task<WorkflowDefinition?> HandleAsync(FindWorkflowDefinitionByDefinitionId request, CancellationToken cancellationToken)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == request.DefinitionId;
        predicate = predicate.WithVersion(request.VersionOptions);
        return await _store.FindAsync(predicate, cancellationToken);
    }

    public async Task<WorkflowDefinition?> HandleAsync(FindWorkflowDefinitionByName request, CancellationToken cancellationToken)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.Name == request.Name;
        predicate = predicate.WithVersion(request.VersionOptions);
        return await _store.FindAsync(predicate, cancellationToken);
    }
}