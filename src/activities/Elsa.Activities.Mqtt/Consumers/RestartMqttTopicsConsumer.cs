using System.Linq;
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
        public Task Handle(TriggersDeleted message) => _mqttTopicsStarter.RemoveWorkersAsync(message.Triggers);
        public Task Handle(BookmarkIndexingFinished message) => _mqttTopicsStarter.CreateWorkersAsync();
        public async Task Handle(BookmarksDeleted message) => await _mqttTopicsStarter.RemoveWorkersAsync(message.Bookmarks, ServiceProvider);
    }
}