using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Triggers;
using MediatR;

namespace Elsa.Handlers
{
    public class UpdateTriggers : INotificationHandler<WorkflowInstanceSaved>, INotificationHandler<ManyWorkflowInstancesDeleted>
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
            await UpdateTriggersAsync(workflowInstance.DefinitionId, workflowInstance.Id, workflowInstance.TenantId, VersionOptions.SpecificVersion(workflowInstance.Version), cancellationToken);
        }

        public async Task Handle(ManyWorkflowInstancesDeleted notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowInstances.First();
            await UpdateTriggersAsync(workflowInstance.DefinitionId, null, workflowInstance.TenantId, VersionOptions.Latest, cancellationToken);
        }

        private async Task UpdateTriggersAsync(string workflowDefinitionId, string? workflowInstanceId, string? tenantId, VersionOptions versionOptions, CancellationToken cancellationToken)
        {
            var workflowBlueprint = await _workflowRegistry.GetWorkflowAsync(workflowDefinitionId, tenantId, versionOptions, cancellationToken);

            if (workflowBlueprint == null)
                return;

            await _workflowSelector.UpdateTriggersAsync(workflowBlueprint, workflowInstanceId, cancellationToken);
        }
    }
}