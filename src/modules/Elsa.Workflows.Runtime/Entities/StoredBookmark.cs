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

    [JsonConstructor]
    public StoredBookmark()
    {
        
    }

    public string ActivityTypeName { get; set; }
    public string Hash { get; set; }
    public string WorkflowInstanceId { get; set; }
    public string BookmarkId { get; set; }
    public string? CorrelationId { get; set; }
    public object? Payload { get; set; }
}