using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Triggers;
using MediatR;

namespace Elsa.Handlers
{
    public class UpdateTriggers : INotificationHandler<WorkflowInstanceSaved>
    {
        private readonly IWorkflowSelector _workflowSelector;
        private readonly IWorkflowRegistry _workflowRegistry;

        public UpdateTriggers(IWorkflowSelector workflowSelector, IWorkflowRegistry workflowRegistry)
        {
            _workflowSelector = workflowSelector;
            _workflowRegistry = workflowRegistry;
        }

        public async Task Handle(WorkflowInstanceSaved notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowInstance;
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(workflowInstance.DefinitionId, workflowInstance.TenantId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);
            
            if (workflowBlueprint == null)
                return;
            
            await _workflowSelector.UpdateTriggersAsync(workflowBlueprint, cancellationToken);
        }
    }
}