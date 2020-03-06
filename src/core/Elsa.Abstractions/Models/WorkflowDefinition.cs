using NodaTime;
using System.Collections.Generic;

namespace Elsa.Models
{
    public class WorkflowDefinition
    {
        public WorkflowDefinition()
        {
            WorkflowDefinitionVersions = new List<WorkflowDefinitionVersion>();
        }
        public string Id { get; set; }
        public string? TenantId { get; set; }
        public Instant CreatedAt { get; set; }
        public ICollection<WorkflowDefinitionVersion> WorkflowDefinitionVersions { get; set; }
    }
}
