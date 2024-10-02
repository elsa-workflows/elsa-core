namespace Elsa.Webhooks.Models;

/// <summary>
/// Stores payload information about the DomainEvent webhook event type.
/// </summary>
public record DomainEventWebhookPayload(
    string WorkflowInstanceId, 
    string WorkflowDefinitionId, 
    string? WorkflowName, 
    string? CorrelationId,
    string DomainEventId, 
    string DomainEventName, 
    object? DomainEventPayload);