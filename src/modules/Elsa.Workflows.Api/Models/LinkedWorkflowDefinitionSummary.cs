using Elsa.Models;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

public class LinkedWorkflowDefinitionSummary : WorkflowDefinitionSummary
{
    public Link[]? Links { get; set; }
}
