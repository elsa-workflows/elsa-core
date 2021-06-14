using System.Threading.Tasks;
using Elsa.Services.Dispatch;
using Quartz;

namespace Elsa.Activities.Temporal.Quartz.Jobs
{
    public class RunQuartzWorkflowInstanceJob : IJob
    {
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;

        public RunQuartzWorkflowInstanceJob(IWorkflowInstanceDispatcher workflowDefinitionDispatcher)
        {
            _workflowInstanceDispatcher = workflowDefinitionDispatcher;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var cancellationToken = context.CancellationToken;
            var workflowInstanceId = dataMap.GetString("WorkflowInstanceId")!;
            var activityId = dataMap.GetString("ActivityId")!;

            await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(workflowInstanceId, activityId), cancellationToken);
        }
    }
}