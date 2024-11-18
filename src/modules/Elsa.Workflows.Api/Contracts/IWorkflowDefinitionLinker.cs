using Elsa.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api;

/// <summary>
/// Maps workflow definition models to liked models
/// </summary>
public interface IWorkflowDefinitionLinker
{
    /// <summary>
    /// Maps to an enhanced model that contains links with the possible operations applicable to a workflow definition.
    /// </summary>
    Task<LinkedWorkflowDefinitionModel> MapAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps a paged list to an enhanced model that contains links with the possible operations applicable to a workflow definition.
    /// </summary>
    PagedListResponse<LinkedWorkflowDefinitionSummary> MapAsync(PagedListResponse<WorkflowDefinitionSummary> list, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maps a list to an enhanced model that contains links with the possible operations applicable to a workflow definition.
    /// </summary>
    Task<List<LinkedWorkflowDefinitionModel>> MapAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken = default);
}