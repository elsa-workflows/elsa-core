using System;
using System.Linq;
using Elsa.Data;
using Elsa.Models;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowInstanceIndex : MapIndex
    {
        public string? TenantId { get; set; }
        public string WorkflowInstanceId { get; set; } = default!;
        public string WorkflowDefinitionId { get; set; } = default!;
        public string? CorrelationId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class WorkflowInstanceBlockingActivitiesIndex : MapIndex
    {
        public string? TenantId { get; set; }
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string? CorrelationId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class WorkflowInstanceIndexProvider : IndexProvider<WorkflowInstance>
    {
        public WorkflowInstanceIndexProvider() => CollectionName = CollectionNames.WorkflowInstances;

        public override void Describe(DescribeContext<WorkflowInstance> context)
        {
            context.For<WorkflowInstanceIndex>()
                .Map(
                    workflowInstance => new WorkflowInstanceIndex
                    {
                        WorkflowInstanceId = workflowInstance.WorkflowInstanceId,
                        WorkflowDefinitionId = workflowInstance.WorkflowDefinitionId,
                        TenantId = workflowInstance.TenantId,
                        WorkflowStatus = workflowInstance.WorkflowStatus,
                        CorrelationId = workflowInstance.CorrelationId,
                        CreatedAt = workflowInstance.CreatedAt.ToDateTimeOffset()
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