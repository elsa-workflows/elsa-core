using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ListConsumingWorkflows;

/// <summary>
/// Lists all workflow definitions that consume the specified workflow definition.
/// </summary>
[UsedImplicitly]
internal class ListConsumingWorkflows(
    IWorkflowDefinitionStore store,
    IWorkflowConsumerService workflowConsumerService) : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Get("/workflow-definitions/{definitionId}/consuming-workflows", "/workflow-definitions/by-version-id/{definitionVersionId}/consuming-workflows");
        ConfigurePermissions("read:workflow-definitions");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // Resolve the workflow definition handle
        var handle = CreateHandle(request);
        
        // Get the workflow definition to extract its DefinitionId
        var filter = handle.ToFilter();
        var definition = await store.FindAsync(filter, cancellationToken);
        
        if (definition == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        
        // Get all workflows that reference this workflow definition (transitive closure)
        var consumingDefinitionIds = await workflowConsumerService
            .GetConsumingWorkflowDefinitionIdsAsync(definition.DefinitionId, cancellationToken)
            .ToListAsync(cancellationToken);
        
        if (!consumingDefinitionIds.Any())
        {
            await Send.OkAsync(new Response(), cancellationToken);
            return;
        }
        
        // Fetch the latest versions of consuming workflows
        var consumingWorkflows = await store.FindSummariesAsync(new WorkflowDefinitionFilter
        {
            DefinitionIds = consumingDefinitionIds.ToArray(),
            VersionOptions = VersionOptions.Latest
        }, cancellationToken);
        
        var response = new Response
        {
            ConsumingWorkflows = consumingWorkflows.ToList()
        };
        
        await Send.OkAsync(response, cancellationToken);
    }

    private WorkflowDefinitionHandle CreateHandle(Request request)
    {
        if (!string.IsNullOrEmpty(request.DefinitionVersionId))
            return WorkflowDefinitionHandle.ByDefinitionVersionId(request.DefinitionVersionId);
        
        if (string.IsNullOrEmpty(request.DefinitionId))
            return null!; // caller must check and return 400
        
        var versionOptions = string.IsNullOrEmpty(request.VersionOptions) 
            ? VersionOptions.Latest 
            : VersionOptions.FromString(request.VersionOptions);
        
        return WorkflowDefinitionHandle.ByDefinitionId(request.DefinitionId, versionOptions);
    }
}
