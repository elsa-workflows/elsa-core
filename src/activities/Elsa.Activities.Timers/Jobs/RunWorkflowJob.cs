using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Activities.Timers.Jobs
{
    public class RunWorkflowJob : IJob
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly ILogger _logger;

        public RunWorkflowJob(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceManager workflowInstanceManager, ILogger<RunWorkflowJob> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceManager = workflowInstanceManager;
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
                if (workflowBlueprint.IsSingleton && await GetWorkflowIsAlreadyExecutingAsync(tenantId, workflowDefinitionId, cancellationToken))
                    return;

                await _workflowRunner.RunWorkflowAsync(workflowBlueprint, activityId, cancellationToken: cancellationToken);
            }
            else
            {
                var workflowInstance = await _workflowInstanceManager.GetByIdAsync(workflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    _logger.LogWarning("Could not run Workflow instance with ID {WorkflowInstanceId} because it appears not yet to be persisted in the database. Skipping this round.", workflowInstanceId);
                    return;
                }
                
                await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance!, activityId, cancellationToken: cancellationToken);
            }
        }

        private async Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId, CancellationToken cancellationToken)
        {
            // See https://github.com/sebastienros/yessql/issues/298

            Expression<Func<WorkflowInstanceIndex, bool>> query = tenantId == null
                ? index =>
                    index.WorkflowDefinitionId == workflowDefinitionId
                    && index.TenantId == null
                    && (index.WorkflowStatus == WorkflowStatus.Running || index.WorkflowStatus == WorkflowStatus.Suspended)
                : index =>
                    index.WorkflowDefinitionId == workflowDefinitionId
                    && index.TenantId == tenantId
                    && (index.WorkflowStatus == WorkflowStatus.Running || index.WorkflowStatus == WorkflowStatus.Suspended);

            var workflowInstance = await _workflowInstanceManager.Query(query).FirstOrDefaultAsync();
            return workflowInstance != null;
        }
    }
}