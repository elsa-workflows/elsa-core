using Elsa.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Services;

/// <summary>
/// Maps workflow definition models to liked models
/// </summary>
public interface IWorkflowDefinitionLinkService
{
    /// <summary>
    /// Maps to an enhanced model that contains links with the possible operations appliable to a workflow definition.
    /// </summary>
    /// <param name="definition"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<LinkedWorkflowDefinitionModel> MapToLinkedWorkflowDefinitionModelAsync(WorkflowDefinition definition, CancellationToken cancellationToken);

    /// <summary>
    /// Maps a paged list to an enhanced model that contains links with the possible operations appliable to a workflow definition.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    PagedListResponse<LinkedWorkflowDefinitionSummary> MapToLinkedPagedListAsync(PagedListResponse<WorkflowDefinitionSummary> list);

    /// <summary>
    /// Maps a list to an enhanced model that contains links with the possible operations appliable to a workflow definition.
    /// </summary>
    /// <param name="definitions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<LinkedWorkflowDefinitionModel>> MapToLinkedListAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken);
}
