namespace Elsa.Bookmarks
{
    public class BookmarkFinderResult
    {
        public BookmarkFinderResult(string workflowInstanceId, string activityId, IBookmark bookmark)
        {
            WorkflowInstanceId = workflowInstanceId;
            ActivityId = activityId;
            Bookmark = bookmark;
        }

        public string WorkflowInstanceId { get; }
        public string ActivityId { get; }
        public IBookmark Bookmark { get; }
    }
}