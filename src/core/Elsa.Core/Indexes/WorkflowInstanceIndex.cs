using System.Linq;
using Elsa.Models;
using NodaTime;
using YesSql.Indexes;

namespace Elsa.Indexes
{
    public class WorkflowInstanceIndex : MapIndex
    {
        public string WorkflowInstanceId { get; set; } = default!;
        public string WorkflowDefinitionId { get; set; } = default!;
        public string? CorrelationId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public Instant CreatedAt { get; set; }
    }

    public class WorkflowInstanceBlockingActivitiesIndex : MapIndex
    {
        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string? CorrelationId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public Instant CreatedAt { get; set; }
    }

    public class WorkflowInstanceIndexProvider : IndexProvider<WorkflowInstance>
    {
        public override void Describe(DescribeContext<WorkflowInstance> context)
        {
            context.For<WorkflowInstanceIndex>()
                .Map(
                    workflowInstance => new WorkflowInstanceIndex
                    {
                        WorkflowInstanceId = workflowInstance.WorkflowInstanceId,
                        WorkflowDefinitionId = workflowInstance.WorkflowDefinitionId,
                        WorkflowStatus = workflowInstance.Status,
                        CorrelationId = workflowInstance.CorrelationId,
                        CreatedAt = workflowInstance.CreatedAt
                    });

            context.For<WorkflowInstanceBlockingActivitiesIndex>()
                .Map(
                    workflowInstance => workflowInstance.BlockingActivities
                        .Select(
                            activity => new WorkflowInstanceBlockingActivitiesIndex
                            {
                                ActivityId = activity.ActivityId,
                                ActivityType = activity.ActivityType,
                                CorrelationId = workflowInstance.CorrelationId,
                                WorkflowStatus = workflowInstance.Status,
                                CreatedAt = workflowInstance.CreatedAt
                            }));
        }
    }
}