namespace Elsa.OrchardCore.WebhookPayloads;

public record ContentItemEventPayload(
    string ContentType,
    string DisplayText,
    string Author,
    string Owner,
    string ContentItemId);