using Elsa.Workflows.Api.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Publish;

internal class Request
{
    public string DefinitionId { get; set; } = default!;
}

internal record Response(LinkedWorkflowDefinitionModel WorkflowDefinition, int ConsumingWorkflowCount);