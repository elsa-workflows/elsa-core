using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Microsoft.Extensions.Logging;

namespace Elsa.Services.Dispatch
{
    /// <summary>
    /// The default strategy that process workflow execution requests by sending them to a queue.
    /// </summary>
    public class QueuingWorkflowDispatcher : IWorkflowDefinitionDispatcher, IWorkflowInstanceDispatcher, IWorkflowDispatcher
    {
        private readonly ICommandSender _commandSender;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly ILogger _logger;
        private readonly WorkflowChannelOptions _workflowChannelOptions;

        public QueuingWorkflowDispatcher(ICommandSender commandSender, IWorkflowInstanceStore workflowInstanceStore, IWorkflowRegistry workflowRegistry, ElsaOptions elsaOptions, ILogger<QueuingWorkflowDispatcher> logger)
        {
            _commandSender = commandSender;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowRegistry = workflowRegistry;
            _workflowChannelOptions = elsaOptions.WorkflowChannelOptions;
            _logger = logger;
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
            await _commandSender.SendAsync(request, queue, cancellationToken: cancellationToken);
        }

        public async Task DispatchAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default) => await _commandSender.SendAsync(request, cancellationToken);

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
            await _commandSender.SendAsync(request, queue, default, cancellationToken);
        }
    }
}