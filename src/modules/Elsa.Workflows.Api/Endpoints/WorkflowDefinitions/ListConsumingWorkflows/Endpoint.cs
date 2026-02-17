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
    IWorkflowReferenceQuery workflowReferenceQuery) : ElsaEndpoint<Request, Response>
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
        var consumingDefinitionIds = await GetAllConsumingWorkflowDefinitionIdsAsync(definition.DefinitionId, cancellationToken);
        
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
        
        var versionOptions = string.IsNullOrEmpty(request.VersionOptions) 
            ? VersionOptions.Latest 
            : VersionOptions.FromString(request.VersionOptions);
        
        return WorkflowDefinitionHandle.ByDefinitionId(request.DefinitionId!, versionOptions);
    }

    private async Task<IEnumerable<string>> GetAllConsumingWorkflowDefinitionIdsAsync(
        string definitionId,
        CancellationToken cancellationToken,
        HashSet<string>? visitedIds = null)
    {
        visitedIds ??= new HashSet<string>();
        var allConsumingIds = new List<string>();

        // If we've already processed this definition ID, skip it to prevent infinite recursion.
        if (!visitedIds.Add(definitionId))
            return allConsumingIds;

        // Get direct references
        var directRefs = await workflowReferenceQuery.ExecuteAsync(definitionId, cancellationToken);
        allConsumingIds.AddRange(directRefs);

        // Recursively get consumers of consumers
        foreach (var refId in directRefs)
        {
            var transitiveRefs = await GetAllConsumingWorkflowDefinitionIdsAsync(refId, cancellationToken, visitedIds);
            allConsumingIds.AddRange(transitiveRefs);
        }

        return allConsumingIds.Distinct();
    }
}
