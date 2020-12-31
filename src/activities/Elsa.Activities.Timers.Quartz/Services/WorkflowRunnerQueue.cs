using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Timers.Quartz.Services
{
    public class WorkflowRunnerQueue
    {
        private readonly IBackgroundWorker _backgroundWorker;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public WorkflowRunnerQueue(IBackgroundWorker backgroundWorker, IServiceProvider serviceProvider, ILogger<WorkflowRunnerQueue> logger)
        {
            _backgroundWorker = backgroundWorker;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        public async Task Enqueue(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default)
        {
            await _backgroundWorker.ScheduleTask(async () => await RunWorkflowAsync(workflowInstanceId, activityId, cancellationToken), cancellationToken);
        }

        private async Task RunWorkflowAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflowInstance = await store.FindByIdAsync(workflowInstanceId, cancellationToken);

            if (workflowInstance == null)
            {
                _logger.LogError("Could not run Workflow instance with ID {WorkflowInstanceId} because it is not in the database", workflowInstanceId);
                return;
            }
                
            //await context.Scheduler.UnscheduleJob(context.Trigger.Key, cancellationToken);
            var workflowDefinitionId = workflowInstance.DefinitionId;
            var tenantId = workflowInstance.TenantId;
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprint = (await workflowRegistry.GetWorkflowAsync(workflowDefinitionId, tenantId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken))!;
            var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
            await workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance!, activityId, cancellationToken: cancellationToken);
        }
    }
}