namespace Elsa.Activities.UserTask.Models;

public record TriggerUserAction(string Action,string? WorkflowInstanceId = default,string? CorrelationId = default );