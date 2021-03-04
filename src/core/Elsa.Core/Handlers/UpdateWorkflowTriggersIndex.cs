using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Triggers;
using MediatR;

namespace Elsa.Handlers
{
    public class UpdateWorkflowTriggersIndex : INotificationHandler<WorkflowDefinitionPublished>
    {
        private readonly ITriggerIndexer _triggerIndexer;

        public UpdateWorkflowTriggersIndex(ITriggerIndexer triggerIndexer)
        {
            _triggerIndexer = triggerIndexer;
        }
        
        public async Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
        {
            await _triggerIndexer.IndexTriggersAsync(cancellationToken);
        }
    }
}