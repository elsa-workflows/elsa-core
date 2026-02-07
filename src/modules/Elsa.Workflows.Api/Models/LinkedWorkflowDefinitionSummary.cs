using Elsa.Models;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

public class LinkedWorkflowDefinitionSummary : WorkflowDefinitionSummary
{
    public Link[]? Links { get; set; }

    /// <summary>
    /// Indicates whether the materializer for this workflow definition is currently available.
    /// </summary>
    public bool IsMaterializerAvailable { get; set; }
}
