namespace Elsa.Services
{
    public class WorkflowTrigger
    {
        public WorkflowTrigger(string workflowDefinitionId, string activityId, string activityType, string bookmarkHash, IBookmark bookmark, string? tenantId)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            ActivityId = activityId;
            ActivityType = activityType;
            BookmarkHash = bookmarkHash;
            Bookmark = bookmark;
            TenantId = tenantId;
        }

        public string WorkflowDefinitionId { get; }
        public string ActivityId { get; }
        public string ActivityType { get; }
        public string BookmarkHash { get; }
        public IBookmark Bookmark { get; }
        public string? TenantId { get; }
    }
}