namespace Elsa.Services
{
    public class TriggerFinderResult
    {
        public TriggerFinderResult(string workflowDefinitionId, string activityId, IBookmark bookmark)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            ActivityId = activityId;
            Bookmark = bookmark;
        }
        public string WorkflowDefinitionId { get; }
        public string ActivityId { get; }
        public IBookmark Bookmark { get; }
    }
}