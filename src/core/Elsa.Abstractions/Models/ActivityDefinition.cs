namespace Elsa.Models
{
    public class ActivityDefinition
    {
        public string ActivityId { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public int? Left { get; set; }
        public int? Top { get; set; }
        public bool PersistWorkflow { get; set; }
        public bool LoadWorkflowContext { get; set; }
        public bool SaveWorkflowContext { get; set; }
        public bool PersistOutput { get; set; }
        public ActivityDefinitionProperties Properties { get; set; } = new();
    }
}