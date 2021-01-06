using System.Collections.Generic;

namespace Elsa.Indexing.Models
{
    public class WorkflowDefinitionIndexModel
    {
        public string Id { get; set; } = default!;
        public string DefinitionVersionId { get; set; } = default!;
        public string? TenantId { get; set; }

        public string? Name { get; set; }

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public int Version { get; set; }

        public bool IsSingleton { get; set; }

        public bool IsEnabled { get; set; }

        public bool IsPublished { get; set; }

        public bool IsLatest { get; set; }

        public List<ActivityDefinitionIndexModel> Activities { get; set; } = new List<ActivityDefinitionIndexModel>();
    }
}
