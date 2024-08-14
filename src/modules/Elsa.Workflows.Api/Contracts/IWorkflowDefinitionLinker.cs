using Elsa.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api;

/// Maps workflow definition models to liked models
public interface IWorkflowDefinitionLinker
{
    /// Maps to an enhanced model that contains links with the possible operations applicable to a workflow definition.
    Task<LinkedWorkflowDefinitionModel> MapAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// Maps a paged list to an enhanced model that contains links with the possible operations applicable to a workflow definition.
    PagedListResponse<LinkedWorkflowDefinitionSummary> MapAsync(PagedListResponse<WorkflowDefinitionSummary> list, CancellationToken cancellationToken = default);

    /// Maps a list to an enhanced model that contains links with the possible operations applicable to a workflow definition.
    Task<List<LinkedWorkflowDefinitionModel>> MapAsync(List<WorkflowDefinition> definitions, CancellationToken cancellationToken = default);
}