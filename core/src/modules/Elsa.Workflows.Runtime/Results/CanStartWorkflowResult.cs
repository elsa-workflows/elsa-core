namespace Elsa.Workflows.Runtime.Results;

public record CanStartWorkflowResult(string? InstanceId, bool CanStart);