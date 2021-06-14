using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services.Triggers;
using Rebus.Handlers;

namespace Elsa.Consumers
{
    public class UpdateWorkflowTriggersIndexConsumer : IHandleMessages<WorkflowDefinitionPublished>, IHandleMessages<WorkflowDefinitionRetracted>, IHandleMessages<WorkflowDefinitionDeleted>
    {
        private readonly ITriggerIndexer _triggerIndexer;
        public UpdateWorkflowTriggersIndexConsumer(ITriggerIndexer triggerIndexer) => _triggerIndexer = triggerIndexer;
        public Task Handle(WorkflowDefinitionPublished message) => _triggerIndexer.IndexTriggersAsync();
        public Task Handle(WorkflowDefinitionRetracted message) => _triggerIndexer.IndexTriggersAsync();
        public Task Handle(WorkflowDefinitionDeleted message) => _triggerIndexer.IndexTriggersAsync();
    }
}