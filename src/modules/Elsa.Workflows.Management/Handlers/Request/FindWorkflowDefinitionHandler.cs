using Elsa.Common.Entities;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Requests;

namespace Elsa.Workflows.Management.Handlers.Request;

public class FindWorkflowDefinitionHandler(IWorkflowDefinitionStore store) : IRequestHandler<FindWorkflowDefinitionRequest, WorkflowDefinition?>
{
    public async Task<WorkflowDefinition?> HandleAsync(FindWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = request.DefinitionId,
            VersionOptions = request.VersionOptions
        };

        var order = new WorkflowDefinitionOrder<int>(x => x.Version, OrderDirection.Descending);
        var definition = (await store.FindManyAsync(filter, order, cancellationToken: cancellationToken)).FirstOrDefault();
        return definition;
    }
}