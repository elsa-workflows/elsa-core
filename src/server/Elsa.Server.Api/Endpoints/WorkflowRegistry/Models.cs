using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Server.Api.Endpoints.WorkflowRegistry
{
    public class WorkflowBlueprintModel : CompositeActivityBlueprintModel
    {
        public int Version { get; set; }
        public string? TenantId { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public bool IsDisabled { get; set; }
        public Variables Variables { get; set; } = new();
        public WorkflowContextOptions? ContextOptions { get; set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedInstances { get; set; }
        public Variables CustomAttributes { get; set; } = new();
    }

    public class CompositeActivityBlueprintModel : ActivityBlueprintModel
    {
        public ICollection<ActivityBlueprintModel> Activities { get; set; } = new List<ActivityBlueprintModel>();
        public ICollection<ConnectionModel> Connections { get; set; } = new List<ConnectionModel>();
    }

    public class ConnectionModel
    {
        public string SourceActivityId { get; set; } = default!;
        public string TargetActivityId { get; set; } = default!;
        public string Outcome { get; set; } = default!;
    }

    public class ActivityBlueprintModel
    {
        public string Id { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; } = default!;
        public string? ParentId { get; set; }
        public bool PersistWorkflow { get; set; }
        public bool LoadWorkflowContext { get; set; }
        public bool SaveWorkflowContext { get; set; }
        public Variables InputProperties { get; set; } = new();
        public Variables OutputProperties { get; set; } = new();
        public int? X { get; set; }
        public int? Y { get; set; }

        public override string ToString() => Type;
    }

    public class WorkflowBlueprintSummaryModel
    {
        public string Id { get; set; } = default!;
        public string VersionId { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public int Version { get; set; }
        public string? TenantId { get; set; }
        public bool IsSingleton { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public bool IsDisabled { get; set; }
    }
}