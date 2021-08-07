namespace Elsa.Services
{
    public class BookmarkFinderResult
    {
        public BookmarkFinderResult(string workflowInstanceId, string activityId, IBookmark bookmark, string? correlationId)
        {
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            Bookmark = bookmark;
            CorrelationId = correlationId;
        }

        public string WorkflowInstanceId { get; }
        public string ActivityId { get; }
        public IBookmark Bookmark { get; }
        public string? CorrelationId { get; }
    }
}