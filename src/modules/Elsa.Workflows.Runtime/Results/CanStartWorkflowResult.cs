namespace Elsa.Workflows.Runtime.Contracts;

public record CanStartWorkflowResult(string? InstanceId, bool CanStart);