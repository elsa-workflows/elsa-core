namespace Elsa.Workflows.Runtime.Contracts;

public record WorkflowsFilter(string ActivityTypeName, object BookmarkPayload, TriggerWorkflowsOptions Options);