using System;
using System.Linq;
using Elsa.Models;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowInstanceIndex : MapIndex
    {
        public string? TenantId { get; set; }
        public string InstanceId { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public string DefinitionVersionId { get; set; } = default!;
        public int Version { get; set; }
        public string? CorrelationId { get; set; }
        public string? ContextType { get; set; }
        public string? ContextId { get; set; }
        public string? Name { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastExecutedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? FaultedAt { get; set; }
    }
    
    public class WorkflowInstanceBlockingActivitiesIndex : MapIndex
    {
        public string? TenantId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string? CorrelationId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WorkflowInstanceIndexProvider : IndexProvider<WorkflowInstanceDocument>
    {
        public WorkflowInstanceIndexProvider() => CollectionName = CollectionNames.WorkflowInstances;

        public override void Describe(DescribeContext<WorkflowInstanceDocument> context)
        {
            context.For<WorkflowInstanceIndex>()
                .Map(
                    workflowInstance => new WorkflowInstanceIndex
                    {
                        TenantId = workflowInstance.TenantId,
                        InstanceId = workflowInstance.InstanceId,
                        DefinitionId = workflowInstance.DefinitionId,
                        DefinitionVersionId = workflowInstance.DefinitionVersionId,
                        Version = workflowInstance.Version,
                        WorkflowStatus = workflowInstance.WorkflowStatus,
                        CorrelationId = workflowInstance.CorrelationId,
                        ContextType = workflowInstance.ContextType,
                        ContextId = workflowInstance.ContextId,
                        Name = workflowInstance.Name,
                        CreatedAt = workflowInstance.CreatedAt.ToDateTimeUtc(),
                        CancelledAt = workflowInstance.CancelledAt?.ToDateTimeUtc(),
                        FinishedAt = workflowInstance.FinishedAt?.ToDateTimeUtc(),
                        LastExecutedAt = workflowInstance.LastExecutedAt?.ToDateTimeUtc(),
                        FaultedAt = workflowInstance.FaultedAt?.ToDateTimeUtc()
                    });
            
            context.For<WorkflowInstanceBlockingActivitiesIndex>()
                .Map(
                    workflowInstance =>
                    {
                        if (workflowInstance.WorkflowStatus != WorkflowStatus.Suspended || !workflowInstance.BlockingActivities.Any())
                            return default;

                        return workflowInstance.BlockingActivities
                            .Select(
                                activity => new WorkflowInstanceBlockingActivitiesIndex
                                {
                                    ActivityId = activity.ActivityId,
                                    ActivityType = activity.ActivityType,
                                    CorrelationId = workflowInstance.CorrelationId,
                                    TenantId = workflowInstance.TenantId,
                                    WorkflowStatus = workflowInstance.WorkflowStatus,
                                    CreatedAt = workflowInstance.CreatedAt.ToDateTimeUtc()
                                });
                    });
        }
    }
}