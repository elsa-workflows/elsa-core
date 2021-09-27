using System.Threading.Tasks;
using Elsa.Services;
using Quartz;

namespace Elsa.Activities.Temporal.Quartz.Jobs
{
    public class RunQuartzWorkflowDefinitionJob : IJob
    {
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        public RunQuartzWorkflowDefinitionJob(IWorkflowDefinitionDispatcher workflowDefinitionDispatcher) => _workflowDefinitionDispatcher = workflowDefinitionDispatcher;

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var cancellationToken = context.CancellationToken;
            var workflowDefinitionId = dataMap.GetString("WorkflowDefinitionId")!;
            var activityId = dataMap.GetString("ActivityId")!;

            await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(workflowDefinitionId, activityId), cancellationToken);
        }
    }
}