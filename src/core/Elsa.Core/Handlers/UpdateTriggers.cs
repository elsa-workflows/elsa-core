using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Triggers;
using MediatR;

namespace Elsa.Handlers
{
    public class UpdateTriggers : INotificationHandler<WorkflowExecuted>
    {
        private readonly IWorkflowSelector _workflowSelector;
        public UpdateTriggers(IWorkflowSelector workflowSelector) => _workflowSelector = workflowSelector;
        public async Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken) => await _workflowSelector.UpdateTriggersAsync(notification.WorkflowExecutionContext.WorkflowBlueprint, cancellationToken);
    }
}