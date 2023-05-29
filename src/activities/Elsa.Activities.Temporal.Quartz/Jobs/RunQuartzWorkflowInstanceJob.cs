using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Activities.Temporal.Quartz.Jobs
{
    public class RunQuartzWorkflowInstanceJob : IJob
    {
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly ILogger _logger;

        public RunQuartzWorkflowInstanceJob(IWorkflowInstanceDispatcher workflowDefinitionDispatcher, ILogger<RunQuartzWorkflowInstanceJob> logger)
        {
            _workflowInstanceDispatcher = workflowDefinitionDispatcher;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var cancellationToken = context.CancellationToken;
            var workflowInstanceId = dataMap.GetString("WorkflowInstanceId")!;
            var activityId = dataMap.GetString("ActivityId")!;

            using var loggingScope = _logger.BeginScope(new WorkflowInstanceLogScope(workflowInstanceId));
            await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(workflowInstanceId, activityId), cancellationToken);
        }
    }
}