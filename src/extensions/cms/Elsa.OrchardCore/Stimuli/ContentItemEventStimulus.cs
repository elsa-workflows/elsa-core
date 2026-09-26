namespace Elsa.OrchardCore.Stimuli;

/// <summary>
/// Represents a stimulus that encapsulates information about a content item event.
/// This record is used to pass data related to events occurring for content items.
/// </summary>
/// <param name="ContentType">The type of content item associated with the event.</param>
/// <param name="EventType">The type of event that occurred for the content item.</param>
public record ContentItemEventStimulus(string ContentType, string EventType);