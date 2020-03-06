using System;
using System.Collections.Generic;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class WorkflowDefinitionEntity
    {
        public string Id { get; set; }
        public string? TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<WorkflowDefinitionVersionEntity> WorkflowDefinitionVersions { get; set; }
    }
}
