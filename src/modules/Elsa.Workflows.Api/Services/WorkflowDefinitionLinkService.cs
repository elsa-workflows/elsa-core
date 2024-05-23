using Elsa.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Api.Services;

public class WorkflowDefinitionLinkService : IWorkflowDefinitionLinkService
{
    private readonly IOptions<ManagementOptions> _managementOptions;
    private readonly WorkflowDefinitionMapper _workflowDefinitionMapper;

    public WorkflowDefinitionLinkService(
        IOptions<ManagementOptions> managementOptions,
        WorkflowDefinitionMapper workflowDefinitionMapper)
    {
        _managementOptions = managementOptions;
        _workflowDefinitionMapper = workflowDefinitionMapper;
    }

    public async Task<LinkedWorkflowDefinitionModel> MapToLinkedWorkflowDefinitionModelAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var workflowDefinitionModel = await _workflowDefinitionMapper.MapAsync(definition, cancellationToken);

        var linkedModel = new LinkedWorkflowDefinitionModel(
            GenerateLinksForSingleEntry(definition.DefinitionId, definition.IsReadonly))
        {
            Id = workflowDefinitionModel.Id,
            DefinitionId = workflowDefinitionModel.DefinitionId,
            Name = workflowDefinitionModel.Name,
            Description = workflowDefinitionModel.Description,
            CreatedAt = workflowDefinitionModel.CreatedAt,
            Version = workflowDefinitionModel.Version,
            ToolVersion = workflowDefinitionModel.ToolVersion,
            Variables = workflowDefinitionModel.Variables,
            Inputs = workflowDefinitionModel.Inputs,
            Outputs = workflowDefinitionModel.Outputs,
            Outcomes = workflowDefinitionModel.Outcomes,
            CustomProperties = workflowDefinitionModel.CustomProperties,
            IsReadonly = workflowDefinitionModel.IsReadonly,
            IsSystem = workflowDefinitionModel.IsSystem,
            IsLatest = workflowDefinitionModel.IsLatest,
            IsPublished = workflowDefinitionModel.IsPublished,
            Options = workflowDefinitionModel.Options,
            UsableAsActivity = workflowDefinitionModel.UsableAsActivity,
            Root = workflowDefinitionModel.Root
        };

        return linkedModel;
    }

    public PagedListResponse<LinkedWorkflowDefinitionSummary> MapToLinkedPagedListAsync(PagedListResponse<WorkflowDefinitionSummary> list)
    {
        var items = new List<LinkedWorkflowDefinitionSummary>();

        foreach (var item in list.Items)
        {
            items.Add(new LinkedWorkflowDefinitionSummary
            {
                Links = GenerateLinksForSingleEntry(item.DefinitionId, item.IsReadonly),
                Id = item.Id,
                DefinitionId = item.DefinitionId,
                Name = item.Name,
                Description = item.Description,
                Version = item.Version,
                ToolVersion = item.ToolVersion,
                IsLatest = item.IsLatest,
                IsPublished = item.IsPublished,
                ProviderName = item.ProviderName,
                MaterializerName = item.MaterializerName,
                CreatedAt = item.CreatedAt,
                IsReadonly = item.IsReadonly
            });
        }

        return new PagedListResponse<LinkedWorkflowDefinitionSummary>
        {
            TotalCount = list.TotalCount,
            Items = items,
            Links = GenerateLinksForPagedList().ToArray()
        };
    }

    public async Task<List<LinkedWorkflowDefinitionModel>> MapToLinkedListAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken)
    {
        var models = (await _workflowDefinitionMapper.MapAsync(definitions, cancellationToken)).ToList();

        var result = new List<LinkedWorkflowDefinitionModel>();

        foreach (var item in models)
        {
            var linkedModel = new LinkedWorkflowDefinitionModel(
            GenerateLinksForSingleEntry(item.DefinitionId, item.IsReadonly))
            {
                Id = item.Id,
                DefinitionId = item.DefinitionId,
                Name = item.Name,
                Description = item.Description,
                CreatedAt = item.CreatedAt,
                Version = item.Version,
                ToolVersion = item.ToolVersion,
                Variables = item.Variables,
                Inputs = item.Inputs,
                Outputs = item.Outputs,
                Outcomes = item.Outcomes,
                CustomProperties = item.CustomProperties,
                IsReadonly = item.IsReadonly,
                IsSystem = item.IsSystem,
                IsLatest = item.IsLatest,
                IsPublished = item.IsPublished,
                Options = item.Options,
                UsableAsActivity = item.UsableAsActivity,
                Root = item.Root
            };

            result.Add(linkedModel);
        }

        return result;
    }

    private Link[] GenerateLinksForPagedList()
    {
        var linksList = new List<Link>
        {
            new Link($"/workflow-definitions", "self", "GET"),
            new Link($"/workflow-definitions/validation/is-name-unique", "is-name-unique", "GET"),
            new Link($"/workflow-definitions/query/count", "count", "GET"),
            new Link($"/workflow-definitions/many-by-id", "many-by-id", "GET")
        };

        if (!_managementOptions.Value.IsReadOnlyMode)
        {
            linksList.Add(new Link($"/bulk-actions/delete/workflow-definitions/by-definition-id", "bulk-delete-by-definition-id", "POST"));
            linksList.Add(new Link($"/bulk-actions/delete/workflow-definitions/by-id", "bulk-delete-by-id", "POST"));
            linksList.Add(new Link($"/bulk-actions/publish/workflow-definitions/by-definition-ids", "bulk-publish", "POST"));
            linksList.Add(new Link($"/bulk-actions/retract/workflow-definitions/by-definition-ids", "bulk-retract", "POST"));
            linksList.Add(new Link($"/workflow-definitions/import", "import", "POST"));
            linksList.Add(new Link($"/workflow-definitions/import-files", "import-files", "POST"));
            linksList.Add(new Link($"/workflow-definitions", "create", "POST"));
        }

        return linksList.ToArray();
    }

    private Link[] GenerateLinksForSingleEntry(string definitionId, bool definitionIsReadonly)
    {
        var links = new List<Link>
        {
            new Link($"/workflow-definitions/{definitionId}", "self", "GET"),
            new Link($"/workflow-definitions/by-definition-id/{definitionId}", "self", "GET"),
            new Link($"/workflow-definitions/{definitionId}/versions", "versions", "GET"),
            new Link($"/workflow-definitions/{definitionId}/bulk-dispatch", "bulk-dispatch", "POST"),
            new Link($"/workflow-definitions/{definitionId}/dispatch", "dispatch", "POST"),
            new Link($"/workflow-definitions/{definitionId}/execute", "execute", "POST"),
            new Link($"/workflow-definitions/{definitionId}/export", "export", "POST")
        };

        if (!_managementOptions.Value.IsReadOnlyMode && !definitionIsReadonly)
        {
            links.Add(new Link($"/workflow-definitions/{definitionId}/publish", "publish", "POST"));
            links.Add(new Link($"/workflow-definitions/{definitionId}/retract", "retract", "POST"));
            links.Add(new Link($"/workflow-definitions/{definitionId}", "delete", "DELETE"));
            links.Add(new Link($"/workflow-definitions/{definitionId}/import", "import", "PUT"));
            links.Add(new Link($"/workflow-definitions/{definitionId}/update-references", "update-references", "POST"));
        }

        return links.ToArray();
    }
}
