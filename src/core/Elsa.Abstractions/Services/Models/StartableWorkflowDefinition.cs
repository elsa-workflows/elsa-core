namespace Elsa.Services.Models
{
    public record StartableWorkflowDefinition(IWorkflowBlueprint WorkflowBlueprint, string? ActivityId, string? CorrelationId = default, string? ContextId = default, bool? isTest = default, string? signalRConnectionId = default);
}