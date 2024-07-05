using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Responses;

/// Represents a response to a request to refresh workflow definitions.
public record RefreshWorkflowDefinitionsResponse(ICollection<WorkflowDefinition> WorkflowDefinitions);