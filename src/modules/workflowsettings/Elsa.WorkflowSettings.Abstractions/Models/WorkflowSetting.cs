using Elsa.Models;

namespace Elsa.WorkflowSettings.Models
{
    public class WorkflowSetting : Entity
    {
        public string WorkflowBlueprintId { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string? Value { get; set; }
    }
}
