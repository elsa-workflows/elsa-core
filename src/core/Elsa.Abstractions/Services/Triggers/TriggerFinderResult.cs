using Elsa.Services.Models;

namespace Elsa.Services
{
    public class TriggerFinderResult
    {
        public TriggerFinderResult(IWorkflowBlueprint workflowBlueprint, string activityId, string activityType, IBookmark bookmark)
        {
            WorkflowBlueprint = workflowBlueprint;
            ActivityId = activityId;
            ActivityType = activityType;
            Bookmark = bookmark;
        }

        public IWorkflowBlueprint WorkflowBlueprint { get; }
        public string ActivityId { get; }
        public string ActivityType { get; }
        public IBookmark Bookmark { get; }
    }
}