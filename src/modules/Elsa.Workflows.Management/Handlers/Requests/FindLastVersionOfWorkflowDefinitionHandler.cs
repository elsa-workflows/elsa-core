using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Requests;

namespace Elsa.Workflows.Management.Handlers.Requests;

public class FindLastVersionOfWorkflowDefinitionHandler(IWorkflowDefinitionStore store) : IRequestHandler<FindLastVersionOfWorkflowDefinitionRequest, WorkflowDefinition?>
{
    public Task<WorkflowDefinition?> HandleAsync(FindLastVersionOfWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter()
        {
            DefinitionId = request.DefinitionId,
        };
        return store.FindLastVersionAsync(filter, cancellationToken: cancellationToken);
    }
}