using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Triggers;
using Rebus.Handlers;

namespace Elsa.Consumers
{
    public class UpdateWorkflowTriggersIndexConsumer : IHandleMessages<WorkflowDefinitionPublished>, IHandleMessages<WorkflowDefinitionRetracted>
    {
        private readonly ITriggerIndexer _triggerIndexer;
        public UpdateWorkflowTriggersIndexConsumer(ITriggerIndexer triggerIndexer) => _triggerIndexer = triggerIndexer;
        public Task Handle(WorkflowDefinitionPublished message) => _triggerIndexer.IndexTriggersAsync();
        public Task Handle(WorkflowDefinitionRetracted message) => _triggerIndexer.IndexTriggersAsync();
    }
}