using Elsa.Models;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Services;

public interface IWorkflowDefinitionLinkService
{
    WorkflowDefinitionModel GenerateLinksForSingleEntry(WorkflowDefinitionModel response);
    PagedListResponse<WorkflowDefinitionSummary> GenerateLinksForPagedListOfEntries(PagedListResponse<WorkflowDefinitionSummary> response);
    List<WorkflowDefinitionModel> GenerateLinksForListOfEntries(List<WorkflowDefinitionModel> response);
}
