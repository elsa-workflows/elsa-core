using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public class TriggerDescriptor
    {
        public IWorkflowBlueprint WorkflowBlueprint { get; set; } = default!;
        public string? WorkflowInstanceId { get; set; }
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public ITrigger Trigger { get; set; } = default!;
    }
}