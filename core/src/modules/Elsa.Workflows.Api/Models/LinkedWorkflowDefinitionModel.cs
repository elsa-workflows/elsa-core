using Elsa.Models;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

public class LinkedWorkflowDefinitionModel(Link[]? links) : WorkflowDefinitionModel
{
    public Link[]? Links { get; init; } = links;
}
