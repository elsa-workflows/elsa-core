using Elsa.MongoDB.Common;

namespace Elsa.MongoDB.Models;

public class StoredBookmark : MongoDocument
{
    public string ActivityTypeName { get; set; }
    public string Hash { get; set; }
    public string WorkflowInstanceId { get; set; }
    public string BookmarkId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Data { get; set; }
}