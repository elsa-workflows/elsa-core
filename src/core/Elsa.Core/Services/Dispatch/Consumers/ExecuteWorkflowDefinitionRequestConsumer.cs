using System.Threading.Tasks;
using Elsa.Models;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Services.Dispatch.Consumers
{
    public class ExecuteWorkflowDefinitionRequestConsumer : IHandleMessages<ExecuteWorkflowDefinitionRequest>
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly ILogger _logger;

        public ExecuteWorkflowDefinitionRequestConsumer(IWorkflowLaunchpad workflowLaunchpad, IWorkflowRegistry workflowRegistry, ILogger<ExecuteWorkflowDefinitionRequestConsumer> logger)
        {
            _workflowLaunchpad = workflowLaunchpad;
            _workflowRegistry = workflowRegistry;
            _logger = logger;
        }

        public async Task Handle(ExecuteWorkflowDefinitionRequest message)
        {
            var workflowDefinitionId = message.WorkflowDefinitionId;
            var tenantId = message.TenantId;
            var workflowBlueprint = await _workflowRegistry.GetAsync(workflowDefinitionId, tenantId, VersionOptions.Published);

            if (workflowBlueprint == null)
            {
                _logger.LogWarning("Could not find workflow with ID {WorkflowDefinitionId}", workflowDefinitionId);
                return;
            }
            
            var startableWorkflow = await _workflowLaunchpad.FindStartableWorkflowAsync(workflowBlueprint, message.ActivityId, message.CorrelationId, message.ContextId, tenantId);

            if (startableWorkflow == null)
            {
                _logger.LogDebug("Could not start workflow with ID {WorkflowDefinitionId}", workflowDefinitionId);
                return;
            }
            
            await _workflowLaunchpad.ExecuteStartableWorkflowAsync(startableWorkflow, message.Input);
        }
    }
}