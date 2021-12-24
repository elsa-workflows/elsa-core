using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.Mqtt.Consumers
{
    public class RestartMqttTopicsConsumer: IHandleMessages<WorkflowDefinitionPublished>, IHandleMessages<WorkflowDefinitionRetracted>, IHandleMessages<WorkflowDefinitionDeleted>
    {
        private readonly IMqttTopicsStarter _mqttTopicsStarter;
        public RestartMqttTopicsConsumer(IMqttTopicsStarter mqttTopicsStarter) => _mqttTopicsStarter = mqttTopicsStarter;
        public Task Handle(WorkflowDefinitionPublished message) => _mqttTopicsStarter.CreateWorkersAsync();
        public Task Handle(WorkflowDefinitionRetracted message) => _mqttTopicsStarter.CreateWorkersAsync();
        public Task Handle(WorkflowDefinitionDeleted message) => _mqttTopicsStarter.CreateWorkersAsync();
    }
}