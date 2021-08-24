﻿using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Persistence.YesSql.Documents
{
    public class WorkflowDefinitionDocument : YesSqlDocument
    {
        public string DefinitionId { get; set; } = default!;
        public string DefinitionVersionId { get; set; } = default!;
        public string? TenantId { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? Channel { get; set; }
        public int Version { get; set; }
        public Variables? Variables { get; set; }
        public Variables? CustomAttributes { get; set; }
        public WorkflowContextOptions? ContextOptions { get; set; }
        public bool IsSingleton { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public string? Tag { get; set; }
        public ICollection<ActivityDefinition> Activities { get; set; } = new List<ActivityDefinition>();
        public ICollection<ConnectionDefinition> Connections { get; set; } = new List<ConnectionDefinition>();
    }
}