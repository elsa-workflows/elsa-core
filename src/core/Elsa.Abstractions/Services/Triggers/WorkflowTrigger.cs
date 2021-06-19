using Elsa.Services.Bookmarks;
using Elsa.Services.Models;

namespace Elsa.Services.Triggers
{
    public class WorkflowTrigger
    {
        public WorkflowTrigger(IWorkflowBlueprint workflowBlueprint, string activityId, string activityType, string bookmarkHash, IBookmark bookmark)
        {
            WorkflowBlueprint = workflowBlueprint;
            ActivityId = activityId;
            ActivityType = activityType;
            BookmarkHash = bookmarkHash;
            Bookmark = bookmark;
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public string ActivityId { get; }
        public string ActivityType { get; }
        public string BookmarkHash { get; }
        public IBookmark Bookmark { get; }
    }
}