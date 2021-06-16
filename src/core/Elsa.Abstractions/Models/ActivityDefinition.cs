using System.Collections.Generic;

namespace Elsa.Models
{
    public class ActivityDefinition
    {
        public string ActivityId { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool PersistWorkflow { get; set; }
        public bool LoadWorkflowContext { get; set; }
        public bool SaveWorkflowContext { get; set; }
        public ICollection<ActivityDefinitionProperty> Properties { get; set; } = new List<ActivityDefinitionProperty>();
        public IDictionary<string, string> PropertyStorageProviders { get; } = new Dictionary<string, string>();
    }
}