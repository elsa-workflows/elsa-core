using System;
using System.Linq;
using Elsa.Models;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowInstanceIndex : MapIndex
    {
        public string WorkflowInstanceId { get; set; }
        public string WorkflowDefinitionId { get; set; }
        public string CorrelationId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WorkflowInstanceBlockingActivitiesIndex : MapIndex
    {
        public string ActivityId { get; set; }
        public string ActivityType { get; set; }
        public string CorrelationId { get; set; }
        public WorkflowStatus WorkflowStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WorkflowInstanceIndexProvider : IndexProvider<WorkflowInstance>
    {
        public override void Describe(DescribeContext<WorkflowInstance> context)
        {
            context.For<WorkflowInstanceIndex>()
                .Map(
                    workflowInstance => new WorkflowInstanceIndex
                    {
                        WorkflowDefinitionId = workflowInstance.Id,
                        WorkflowStatus = workflowInstance.Status,
                        CorrelationId = workflowInstance.CorrelationId,
                        CreatedAt = workflowInstance.CreatedAt.ToDateTimeUtc()
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
                                CreatedAt = workflowInstance.CreatedAt.ToDateTimeUtc()
                            }));
        }
    }
}