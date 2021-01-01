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
    // TODO: Consider turning this into a global service to allow background, sequential execution of a given workflow instance (but allow for parallel execution of workflow instances with different definitions). Executing the same workflow instances sequentially prevents loss of data during update concurrency conflicts.
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

            if (!ValidatePreconditions(workflowInstanceId, workflowInstance))
                return;

            _logger.LogDebug("Running {WorkflowInstanceId} with status {WorkflowStatus}.", workflowInstance!.WorkflowStatus);
            
            var workflowDefinitionId = workflowInstance!.DefinitionId;
            var tenantId = workflowInstance.TenantId;
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprint = (await workflowRegistry.GetWorkflowAsync(workflowDefinitionId, tenantId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken))!;
            var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
            await workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance!, activityId, cancellationToken: cancellationToken);
        }

        private bool ValidatePreconditions(string? workflowInstanceId, WorkflowInstance? workflowInstance)
        {
            if (workflowInstance == null)
            {
                _logger.LogError("Could not run workflow instance with ID {WorkflowInstanceId} because it does not exist.", workflowInstanceId);
                return false;
            }

            if (workflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
            {
                _logger.LogWarning("Could not run workflow instance with ID {WorkflowInstanceId} because it has a status other than Suspended. Its actual status is {WorkflowStatus}", workflowInstanceId, workflowInstance.WorkflowStatus);
                return false;
            }

            return true;
        }
    }
}