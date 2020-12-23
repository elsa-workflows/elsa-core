using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Activities.Timers.Quartz.Jobs
{
    public class RunQuartzWorkflowJob : IJob
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceManager;
        private readonly ILogger _logger;

        public RunQuartzWorkflowJob(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore, ILogger<RunQuartzWorkflowJob> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceManager = workflowInstanceStore;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var tenantId = dataMap.GetString("TenantId");
            var workflowDefinitionId = dataMap.GetString("WorkflowDefinitionId")!;
            var workflowInstanceId = dataMap.GetString("WorkflowInstanceId");
            var activityId = dataMap.GetString("ActivityId")!;
            var cancellationToken = context.CancellationToken;
            var workflowBlueprint = (await _workflowRegistry.GetWorkflowAsync(workflowDefinitionId, tenantId, VersionOptions.Published, cancellationToken))!;

            if (workflowInstanceId == null)
            {
                if (workflowBlueprint.IsSingleton)
                {
                    var hasExecutingWorkflow = (await _workflowInstanceManager.FindAsync(new WorkflowIsAlreadyExecutingSpecification().WithTenant(tenantId).WithWorkflowDefinition(workflowDefinitionId), cancellationToken)) != null;

                    if (hasExecutingWorkflow)
                        return;
                }

                await _workflowRunner.RunWorkflowAsync(workflowBlueprint, activityId, cancellationToken: cancellationToken);
            }
            else
            {
                var workflowInstance = await _workflowInstanceManager.FindByIdAsync(workflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    _logger.LogError("Could not run Workflow instance with ID {WorkflowInstanceId} because it is not in the database", data.WorkflowInstanceId);
                    return;
                }

                await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance!, activityId, cancellationToken: cancellationToken);
            }
        }

        private async Task<WorkflowInstance?> GetWorkflowInstanceAsync(string workflowInstanceId, CancellationToken cancellationToken)
        {
            WorkflowInstance? workflowInstance = null;

            for (var i = 0; i < TimerConsts.MaxRetrayGetWorkflow && workflowInstance == null; i++)
            {
                workflowInstance = await _workflowInstanceManager.GetByIdAsync(workflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    System.Threading.Thread.Sleep(10000);
                }
            }

            return workflowInstance;
        }
    }
}