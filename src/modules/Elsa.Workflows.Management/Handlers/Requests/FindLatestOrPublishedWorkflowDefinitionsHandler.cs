using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Requests;

namespace Elsa.Workflows.Management.Handlers.Requests;

public class FindLatestOrPublishedWorkflowDefinitionsHandler(IWorkflowDefinitionStore store) : IRequestHandler<FindLatestOrPublishedWorkflowDefinitionsRequest, ICollection<WorkflowDefinition>>
{
    public Task<WorkflowDefinition?> HandleAsync(FindLastVersionOfWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter()
        {
            DefinitionId = request.DefinitionId,
        };
        return store.FindLastVersionAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<ICollection<WorkflowDefinition>> HandleAsync(FindLatestOrPublishedWorkflowDefinitionsRequest request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = VersionOptions.LatestOrPublished
        };
        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        return (await store.FindManyAsync(filter, order, cancellationToken)).ToList();
    }
}