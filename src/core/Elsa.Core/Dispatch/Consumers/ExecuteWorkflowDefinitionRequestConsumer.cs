using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Dispatch.Consumers
{
    public class ExecuteWorkflowDefinitionRequestConsumer : IHandleMessages<ExecuteWorkflowDefinitionRequest>
    {
        private readonly IStartsWorkflow _startsWorkflow;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly ILogger _logger;

        public ExecuteWorkflowDefinitionRequestConsumer(
            IStartsWorkflow startsWorkflow,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            ILogger<ExecuteWorkflowDefinitionRequestConsumer> logger)
        {
            _startsWorkflow = startsWorkflow;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _logger = logger;
        }

        public async Task Handle(ExecuteWorkflowDefinitionRequest message)
        {
            var workflowDefinitionId = message.WorkflowDefinitionId;
            var tenantId = message.TenantId;
            var workflowBlueprint = await _workflowRegistry.GetAsync(workflowDefinitionId, tenantId, VersionOptions.Published);

            if (!ValidatePreconditions(workflowDefinitionId, workflowBlueprint))
                return;
            
            if (!workflowBlueprint!.IsSingleton || await GetWorkflowIsAlreadyExecutingAsync(tenantId, workflowDefinitionId) == false)
                await _startsWorkflow.StartWorkflowAsync(workflowBlueprint, message.ActivityId, message.Input, message.CorrelationId, message.ContextId);
        }

        private bool ValidatePreconditions(string? workflowDefinitionId, IWorkflowBlueprint? workflowBlueprint)
        {
            if (workflowBlueprint == null)
            {
                _logger.LogWarning("No workflow definition {WorkflowDefinitionId} found. Make sure the scheduled workflow definition is published and enabled", workflowDefinitionId);
                return false;
            }

            return true;
        }
        
        private async Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId)
        {
            var specification = new TenantSpecification<WorkflowInstance>(tenantId).WithWorkflowDefinition(workflowDefinitionId).And(new WorkflowIsAlreadyExecutingSpecification());
            return await _workflowInstanceStore.FindAsync(specification) != null;
        }
    }
}