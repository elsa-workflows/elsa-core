using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.Hangfire.Jobs;
using Elsa.Services;
using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Logging;

namespace Elsa.Server.Hangfire.Dispatch
{
    public class HangfireWorkflowDispatcher : IWorkflowDefinitionDispatcher, IWorkflowInstanceDispatcher, IWorkflowDispatcher
    {
        private readonly IBackgroundJobClient _jobClient;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly ILogger _logger;
        private readonly WorkflowChannelOptions _workflowChannelOptions;

        public HangfireWorkflowDispatcher(IBackgroundJobClient jobClient, IWorkflowInstanceStore workflowInstanceStore, IWorkflowRegistry workflowRegistry, ElsaOptions elsaOptions, ILogger<HangfireWorkflowDispatcher> logger)
        {
            _jobClient = jobClient;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowRegistry = workflowRegistry;
            _workflowChannelOptions = elsaOptions.WorkflowChannelOptions;
            _logger = logger;
        }

        public async Task DispatchAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.GetAsync(request.WorkflowDefinitionId, request.TenantId, VersionOptions.Published, cancellationToken);

            if (workflowBlueprint == null)
            {
                _logger.LogWarning("No published version found for workflow blueprint {WorkflowDefinitionId}", request.WorkflowDefinitionId);
                return;
            }

            var channel = _workflowChannelOptions.GetChannelOrDefault(workflowBlueprint.Channel);
            var queue = ElsaOptions.FormatChannelQueueName<ExecuteWorkflowDefinitionRequest>(channel);
            EnqueueJob<WorkflowDefinitionJob>(x => x.ExecuteAsync(request, CancellationToken.None), queue);
        }

        public async Task DispatchAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(request.WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
            {
                _logger.LogWarning("Cannot dispatch a workflow instance ID that does not exist");
                return;
            }

            var workflowBlueprint = await _workflowRegistry.GetAsync(workflowInstance.DefinitionId, workflowInstance.TenantId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);

            if (workflowBlueprint == null)
            {
                _logger.LogWarning("Workflow instance {WorkflowInstanceId} references workflow blueprint {WorkflowDefinitionId} with version {Version}, but could not be found",
                    workflowInstance.Id,
                    workflowInstance.DefinitionId,
                    workflowInstance.Version);

                return;
            }

            var channel = _workflowChannelOptions.GetChannelOrDefault(workflowBlueprint.Channel);
            var queue = ElsaOptions.FormatChannelQueueName<ExecuteWorkflowInstanceRequest>(channel);
            EnqueueJob<WorkflowInstanceJob>(x => x.ExecuteAsync(request, CancellationToken.None), queue);
        }

        public Task DispatchAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
        {
            EnqueueJob<CorrelatedWorkflowDefinitionJob>(x => x.ExecuteAsync(request, CancellationToken.None), QueueNames.CorrelatedWorkflows);
            return Task.CompletedTask;
        }

        private void EnqueueJob<T>(Expression<Func<T, Task>> job, string queueName) => _jobClient.Create(job, new EnqueuedState(queueName));
    }
}