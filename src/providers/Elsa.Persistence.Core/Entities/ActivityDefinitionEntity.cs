using Elsa.Models;

namespace Elsa.Persistence.Core.Entities
{
    public class ActivityDefinitionEntity
    {
        public int Id { get; set; }
        public string ActivityId { get; set; } = default!;
        public WorkflowDefinitionVersionEntity WorkflowDefinitionVersion { get; set; } = default!;
        public string Type { get; set; }= default!;
        public string Name { get; set; }= default!;
        public string DisplayName { get; set; }= default!;
        public string Description { get; set; }= default!;
        public int Left { get; set; }
        public int Top { get; set; }
        public Variables State { get; set; }= default!;
    }
}