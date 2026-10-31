using Elsa.Scheduling.Activities;

namespace Elsa.Scheduling.Bookmarks;

/// <summary>
/// A bookmark payload for <see cref="Delay"/>.
/// </summary>
public record DelayPayload(DateTimeOffset ResumeAt);