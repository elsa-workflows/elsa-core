using Elsa.Models;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

public record LinkedWorkflowDefinitionModel(Link[]? Links) : WorkflowDefinitionModel;
