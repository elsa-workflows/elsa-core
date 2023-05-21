using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.Mqtt.Consumers
{
    public class RestartMqttTopicsConsumer : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IMqttTopicsStarter _mqttTopicsStarter;
        public RestartMqttTopicsConsumer(IMqttTopicsStarter mqttTopicsStarter) => _mqttTopicsStarter = mqttTopicsStarter;
        public Task Handle(TriggerIndexingFinished message) => _mqttTopicsStarter.CreateWorkersAsync();
        public Task Handle(TriggersDeleted message) => _mqttTopicsStarter.CreateWorkersAsync(message.WorkflowDefinitionId);
        public Task Handle(BookmarkIndexingFinished message) => _mqttTopicsStarter.CreateWorkersAsync();
        public Task Handle(BookmarksDeleted message) => _mqttTopicsStarter.CreateWorkersAsync();
    }
}