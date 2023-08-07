namespace Elsa.Telnyx.Bookmarks;

/// <summary>
/// A bookmark payload for Telnyx webhook events.
/// </summary>
/// <param name="EventType">The event type.</param>
/// <param name="CallControlId">An optional call control ID.</param>
/// <param name="ActivityInstanceId">An optional activity instance ID.</param>
public record WebhookEventBookmarkPayload(string EventType, string? CallControlId = default, string? ActivityInstanceId = default);