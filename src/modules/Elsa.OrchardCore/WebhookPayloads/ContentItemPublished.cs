namespace Elsa.OrchardCore.WebhookPayloads;

public record ContentItemPublished(string PublishedContentItemId, string? PreviousContentItemId);