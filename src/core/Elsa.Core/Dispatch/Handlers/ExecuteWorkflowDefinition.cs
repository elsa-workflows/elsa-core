using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Dispatch.Handlers
{
    public class ExecuteWorkflowDefinition : IRequestHandler<ExecuteWorkflowDefinitionRequest>
    {
        private readonly IStartsWorkflow _startsWorkflow;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly ILogger _logger;

        public ExecuteWorkflowDefinition(
            IStartsWorkflow startsWorkflow,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            ILogger<ExecuteWorkflowDefinition> logger)
        {
            _startsWorkflow = startsWorkflow;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _logger = logger;
        }

        public async Task<Unit> Handle(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken)
        {
            var workflowDefinitionId = request.WorkflowDefinitionId;
            var tenantId = request.TenantId;
            var workflowBlueprint = await _workflowRegistry.GetAsync(workflowDefinitionId, tenantId, VersionOptions.Published, cancellationToken);

            if (!ValidatePreconditions(workflowDefinitionId, workflowBlueprint))
                return Unit.Value;
            
            if (!workflowBlueprint!.IsSingleton || await GetWorkflowIsAlreadyExecutingAsync(tenantId, workflowDefinitionId) == false)
                await _startsWorkflow.StartWorkflowAsync(workflowBlueprint, request.ActivityId, request.Input, request.CorrelationId, request.ContextId, cancellationToken);
            
            return Unit.Value;
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