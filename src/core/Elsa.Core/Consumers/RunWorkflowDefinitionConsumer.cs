using System.Threading.Tasks;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Consumers
{
    public class RunWorkflowDefinitionConsumer : IHandleMessages<RunWorkflowDefinition>
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly ILogger _logger;

        public RunWorkflowDefinitionConsumer(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, ILogger<RunWorkflowInstanceConsumer> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _logger = logger;
        }

        public async Task Handle(RunWorkflowDefinition message)
        {
            var workflowDefinitionId = message.WorkflowDefinitionId;
            var tenantId = message.TenantId;
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(workflowDefinitionId, tenantId, VersionOptions.Published);

            if (!ValidatePreconditions(workflowDefinitionId, workflowBlueprint))
                return;
            
            await _workflowRunner.RunWorkflowAsync(workflowBlueprint!, message.ActivityId, message.Input, message.CorrelationId, message.ContextId);
        }
        
        private bool ValidatePreconditions(string? workflowDefinitionId, IWorkflowBlueprint? workflowBlueprint)
        {
            if (workflowBlueprint == null)
            {
                _logger.LogError("Could not run workflow with ID {WorkflowDefinitionId} because it does not exist.", workflowDefinitionId);
                return false;
            }
            
            return true;
        }
    }
}