namespace Elsa.Bookmarks
{
    public class WorkflowSelectorResult
    {
        public WorkflowSelectorResult(string workflowInstanceId, string activityId, IBookmark bookmark)
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