namespace Elsa.Telnyx.Bookmarks;

/// <summary>
/// A bookmark payload for the call.answered Telnyx webhook event.
/// </summary>
/// <param name="CallControlId">The call control ID.</param>
/// <param name="ActivityInstanceId">The activity instance ID.</param>
public record AnswerCallBookmarkPayload(string CallControlId, string ActivityInstanceId);