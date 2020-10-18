using System.Linq;
using Elsa.Data;
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
        public WorkflowInstanceByReceiveHttpRequestIndexProvider() => CollectionName = CollectionNames.WorkflowInstances;

        public override void Describe(DescribeContext<WorkflowInstance> context)
        {
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