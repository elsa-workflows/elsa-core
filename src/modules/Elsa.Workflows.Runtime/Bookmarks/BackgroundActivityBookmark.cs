namespace Elsa.Workflows.Runtime.Bookmarks;

/// <summary>
/// A bookmark that is used to resume a workflow that is waiting for a background activity to complete.
/// </summary>
public class BackgroundActivityBookmark
{
    /// <summary>
    /// Set retroactively after the job has been scheduled. It is used to cancel te job when the bookmark is deleted.
    /// </summary>
    public string? JobId { get; set; }
}