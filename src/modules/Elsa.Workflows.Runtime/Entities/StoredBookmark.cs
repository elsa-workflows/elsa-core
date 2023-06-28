using System.Text.Json.Serialization;

namespace Elsa.Workflows.Runtime.Entities;

/// <summary>
/// Represents a bookmark that has been stored in the database.
/// </summary>
public class StoredBookmark
{
    /// <summary>
    /// Represents a bookmark that has been stored in the database.
    /// </summary>
    public StoredBookmark(
        string activityTypeName, 
        string hash, 
        string workflowInstanceId, 
        string bookmarkId, 
        string? correlationId = default,
        object? payload = default)
    {
        ActivityTypeName = activityTypeName;
        Hash = hash;
        WorkflowInstanceId = workflowInstanceId;
        BookmarkId = bookmarkId;
        CorrelationId = correlationId;
        Payload = payload;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoredBookmark"/> class.
    /// </summary>
    [JsonConstructor]
    public StoredBookmark()
    {
    }

    /// <summary>
    /// The name of the activity type associated with the bookmark.
    /// </summary>
    public string ActivityTypeName { get; set; } = default!;
    
    /// <summary>
    /// The hash of the bookmark.
    /// </summary>
    public string Hash { get; set; } = default!;
    
    /// <summary>
    /// The ID of the workflow instance associated with the bookmark.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;
    
    /// <summary>
    /// The ID of the bookmark.
    /// </summary>
    public string BookmarkId { get; set; } = default!;
    
    /// <summary>
    /// The correlation ID of the workflow instance associated with the bookmark.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// The data associated with the bookmark.
    /// </summary>
    public object? Payload { get; set; }
}