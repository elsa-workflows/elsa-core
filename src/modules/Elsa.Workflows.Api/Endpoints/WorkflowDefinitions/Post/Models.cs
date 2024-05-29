using Elsa.Workflows.Api.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Post;

internal record Response(LinkedWorkflowDefinitionModel WorkflowDefinition, int ConsumingWorkflowCount);