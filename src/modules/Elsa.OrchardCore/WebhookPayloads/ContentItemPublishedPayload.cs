namespace Elsa.OrchardCore.WebhookPayloads;

public record ContentItemPublishedPayload(
    string ContentType,
    string DisplayText,
    string Author,
    string Owner,
    string PublishedContentItemId,
    string? PreviousContentItemId);