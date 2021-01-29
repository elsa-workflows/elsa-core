using Elsa.Services.Models;

namespace Elsa.Bookmarks
{
    public class WorkflowStartTriggerDescriptor
    {
        public IWorkflowBlueprint WorkflowBlueprint { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
    }
}