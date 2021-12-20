using Elsa.Activities.RabbitMq.Services;
using Elsa.Events;
using Rebus.Handlers;
using System.Threading.Tasks;

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
