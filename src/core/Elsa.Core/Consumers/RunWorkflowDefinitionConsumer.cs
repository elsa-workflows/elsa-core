using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
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
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ILogger _logger;

        public RunWorkflowDefinitionConsumer(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore, IBookmarkFinder bookmarkFinder, ILogger<RunWorkflowDefinitionConsumer> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _bookmarkFinder = bookmarkFinder;
            _logger = logger;
        }

        public async Task Handle(RunWorkflowDefinition message)
        {
            var workflowDefinitionId = message.WorkflowDefinitionId;
            var tenantId = message.TenantId;
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(workflowDefinitionId, tenantId, VersionOptions.Published);

            if (!ValidatePreconditions(workflowDefinitionId, workflowBlueprint))
                return;

            var correlationId = message.CorrelationId;
            
            if (!string.IsNullOrWhiteSpace(message.CorrelationId))
            {
                var correlatedWorkflowInstanceCount = await _workflowInstanceStore.CountAsync(new CorrelationIdSpecification<WorkflowInstance>(correlationId));

                if (correlatedWorkflowInstanceCount > 0)
                {
                    // Do not create a new workflow instance.
                    _logger.LogWarning("There's already a workflow with correlation ID '{CorrelationId}'", correlationId);
                    return;
                }
                
                _logger.LogDebug("No existing workflows found with correlation ID '{CorrelationId}'. Starting new workflow", correlationId);
            }
            
            await _workflowRunner.RunWorkflowAsync(workflowBlueprint!, message.ActivityId, message.Input, message.CorrelationId, message.ContextId);
        }
        
        private bool ValidatePreconditions(string? workflowDefinitionId, IWorkflowBlueprint? workflowBlueprint)
        {
            if (workflowBlueprint == null)
            {
                _logger.LogError("Could not run workflow with ID {WorkflowDefinitionId} because it does not exist", workflowDefinitionId);
                return false;
            }
            
            return true;
        }
    }
}