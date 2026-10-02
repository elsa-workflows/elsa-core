namespace Elsa.OrchardCore.WebhookPayloads;

/// <summary>
/// Represents the payload for events related to Orchard Core content items.
/// The payload includes information about the content item such as its type, display text, author, owner, and unique identifier.
/// This record is commonly used with events like creation, publication, unpublication, and deletion of content items.
/// </summary>
public record ContentItemEventPayload(
    string ContentType,
    string DisplayText,
    string Author,
    string Owner,
    string ContentItemId);