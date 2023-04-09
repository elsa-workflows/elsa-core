using System.Text.Json.Serialization;

namespace Elsa.Workflows.Runtime.Models.Bookmarks;

/// <summary>
/// A bookmark that is used to resume a workflow that is waiting for a background activity to complete.
/// </summary>
public class BackgroundActivityBookmark
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundActivityBookmark"/> class.
    /// </summary>
    [JsonConstructor]
    public BackgroundActivityBookmark()
    {
    }
}