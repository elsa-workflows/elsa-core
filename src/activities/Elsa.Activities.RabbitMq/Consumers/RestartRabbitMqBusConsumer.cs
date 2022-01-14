using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.RabbitMq.Consumers
{
    public class RestartRabbitMqBusConsumer : IHandleMessages<WorkflowDefinitionPublished>, IHandleMessages<WorkflowDefinitionRetracted>
    {
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        public RestartRabbitMqBusConsumer(IRabbitMqQueueStarter rabbitMqQueueStarter) => _rabbitMqQueueStarter = rabbitMqQueueStarter;
        public Task Handle(WorkflowDefinitionPublished message) => _rabbitMqQueueStarter.CreateWorkersAsync();
        public Task Handle(WorkflowDefinitionRetracted message) => _rabbitMqQueueStarter.CreateWorkersAsync();
    }
}
