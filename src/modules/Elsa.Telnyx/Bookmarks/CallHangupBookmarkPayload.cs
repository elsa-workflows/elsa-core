namespace Elsa.Telnyx.Bookmarks;

/// <summary>
/// A bookmark payload for the call.hangup Telnyx webhook event.
/// </summary>
/// <param name="CallControlId"></param>
public record CallHangupBookmarkPayload(string CallControlId);