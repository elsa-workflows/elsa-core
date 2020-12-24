using System;
using System.Linq;
using Elsa.Data;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowInstanceIndex : MapIndex
    {
        public string? TenantId { get; set; }
        public string InstanceId { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;
        public int Version { get; set; }
        public string? CorrelationId { get; set; }
        public string? ContextId { get; set; }
        public string? Name { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastExecutedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset? FaultedAt { get; set; }
    }
    
    public class WorkflowInstanceBlockingActivitiesIndex : MapIndex
    {
        public string? TenantId { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string? CorrelationId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
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
                        Version = workflowInstance.Version,
                        WorkflowStatus = workflowInstance.WorkflowStatus,
                        CorrelationId = workflowInstance.CorrelationId,
                        ContextId = workflowInstance.ContextId,
                        Name = workflowInstance.Name,
                        CreatedAt = workflowInstance.CreatedAt.ToDateTimeOffset(),
                        CancelledAt = workflowInstance.CancelledAt?.ToDateTimeOffset(),
                        FinishedAt = workflowInstance.FinishedAt?.ToDateTimeOffset(),
                        LastExecutedAt = workflowInstance.LastExecutedAt?.ToDateTimeOffset(),
                        FaultedAt = workflowInstance.FaultedAt?.ToDateTimeOffset()
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
                                    CreatedAt = workflowInstance.CreatedAt.ToDateTimeOffset()
                                });
                    });
        }
    }
}