using System.Linq;
using Elsa.Indexes;
using Elsa.Models;
using YesSql.Indexes;

namespace Elsa.Activities.Http.Indexes
{
    public class WorkflowInstanceByReceiveHttpRequestIndex : MapIndex
    {
        public string ActivityId { get; set; } = default!;
        public string RequestPath { get; set; } = default!;
        public string? RequestMethod { get; set; }
    }

    public class WorkflowInstanceByReceiveHttpRequestIndexProvider : IndexProvider<WorkflowInstance>
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

            context.For<WorkflowInstanceByReceiveHttpRequestIndex>()
                .Map(
                    workflowInstance => workflowInstance.BlockingActivities
                        .Where(x => x.ActivityType == nameof(ReceiveHttpRequest))
                        .Select(
                            blockingActivity =>
                            {
                                var activity = workflowInstance.Activities
                                    .First(x => x.Id == blockingActivity.ActivityId);

                                var path = activity.Data.Value<string>(nameof(ReceiveHttpRequest.Path));
                                var method = activity.Data.Value<string>(nameof(ReceiveHttpRequest.Method));

                                return new WorkflowInstanceByReceiveHttpRequestIndex
                                {
                                    ActivityId = blockingActivity.ActivityId,
                                    RequestPath = path,
                                    RequestMethod = method
                                };
                            }));
        }
    }
}