using Elsa.Models;
using Elsa.Workflows.Api.Options;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Api.Services;

public class WorkflowDefinitionLinkService : IWorkflowDefinitionLinkService
{
    private readonly IOptions<ApiOptions> _apiOptions;

    public WorkflowDefinitionLinkService(IOptions<ApiOptions> apiOptions)
    {
        _apiOptions = apiOptions;
    }

    public WorkflowDefinitionModel GenerateLinksForSingleEntry(WorkflowDefinitionModel response)
    {
        response.Links.AddRange(HandleGenerateLinksForSingleEntry(response.DefinitionId, response.IsReadonly));

        return response;
    }

    public List<WorkflowDefinitionModel> GenerateLinksForListOfEntries(List<WorkflowDefinitionModel> response)
    {
        foreach (var item in response)
        {
            item.Links.AddRange(HandleGenerateLinksForSingleEntry(item.DefinitionId, item.IsReadonly));
        }

        return response;
    }

    public PagedListResponse<WorkflowDefinitionSummary> GenerateLinksForPagedListOfEntries(PagedListResponse<WorkflowDefinitionSummary> response)
    {
        response.Links.Add(new Link($"/workflow-definitions", "self", "GET"));
        response.Links.Add(new Link($"/workflow-definitions/validation/is-name-unique", "is-name-unique", "GET"));
        response.Links.Add(new Link($"/workflow-definitions/query/count", "count", "GET"));
        response.Links.Add(new Link($"/workflow-definitions/many-by-id", "many-by-id", "GET"));

        if (!_apiOptions.Value.IsReadOnlyMode)
        {
            response.Links.Add(new Link($"/bulk-actions/delete/workflow-definitions/by-definition-id", "bulk-delete-by-definition-id", "POST"));
            response.Links.Add(new Link($"/bulk-actions/delete/workflow-definitions/by-id", "bulk-delete-by-id", "POST"));
            response.Links.Add(new Link($"/bulk-actions/publish/workflow-definitions/by-definition-ids", "bulk-publish", "POST"));
            response.Links.Add(new Link($"/bulk-actions/retract/workflow-definitions/by-definition-ids", "bulk-retract", "POST"));
            response.Links.Add(new Link($"/workflow-definitions/import", "import", "POST"));
            response.Links.Add(new Link($"/workflow-definitions/import-files", "import-files", "POST"));
            response.Links.Add(new Link($"/workflow-definitions", "create", "POST"));
        }

        foreach (var item in response.Items)
        {
            item.Links.AddRange(HandleGenerateLinksForSingleEntry(item.DefinitionId, item.IsReadonly));
        }

        return response;
    }

    private List<Link> HandleGenerateLinksForSingleEntry(string definitionId, bool definitionIsReadonly)
    {
        var links = new List<Link>();

        links.Add(new Link($"/workflow-definitions/{definitionId}", "self", "GET"));
        links.Add(new Link($"/workflow-definitions/by-definition-id/{definitionId}", "self", "GET"));
        links.Add(new Link($"/workflow-definitions/{definitionId}/versions", "versions", "GET"));

        if (!_apiOptions.Value.IsReadOnlyMode && !definitionIsReadonly)
        {
            links.Add(new Link($"/workflow-definitions/{definitionId}/publish", "publish", "POST"));
            links.Add(new Link($"/workflow-definitions/{definitionId}/retract", "retract", "POST"));
            links.Add(new Link($"/workflow-definitions/{definitionId}", "delete", "DELETE"));
            links.Add(new Link($"/workflow-definitions/{definitionId}/import", "import", "PUT"));
            links.Add(new Link($"/workflow-definitions/{definitionId}/update-references", "update-references", "POST"));
        }

        links.Add(new Link($"/workflow-definitions/{definitionId}/bulk-dispatch", "bulk-dispatch", "POST"));
        links.Add(new Link($"/workflow-definitions/{definitionId}/dispatch", "dispatch", "POST"));
        links.Add(new Link($"/workflow-definitions/{definitionId}/execute", "execute", "POST"));
        links.Add(new Link($"/workflow-definitions/{definitionId}/export", "export", "POST"));

        return links;
    }
}
