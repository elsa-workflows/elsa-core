namespace Elsa.Telnyx.Bookmarks;

/// <summary>
/// A bookmark payload for the call.answered Telnyx webhook event.
/// </summary>
/// <param name="CallControlId"></param>
public record CallAnsweredBookmarkPayload(string CallControlId);