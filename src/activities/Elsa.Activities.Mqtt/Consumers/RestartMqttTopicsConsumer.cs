using System;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.Mqtt.Consumers
{
    public class RestartMqttTopicsConsumer : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IMqttTopicsStarter _mqttTopicsStarter;
        private readonly IServiceProvider _services;

        public RestartMqttTopicsConsumer(IMqttTopicsStarter mqttTopicsStarter, IServiceProvider services)
        {
            _mqttTopicsStarter = mqttTopicsStarter;
            _services = services;
        }

        public Task Handle(TriggerIndexingFinished message) => _mqttTopicsStarter.CreateWorkersAsync(message.Triggers, _services);
        public Task Handle(TriggersDeleted message) => _mqttTopicsStarter.RemoveWorkersAsync(message.Triggers);
        public Task Handle(BookmarkIndexingFinished message) => _mqttTopicsStarter.CreateWorkersAsync(message.Bookmarks, _services);
        public Task Handle(BookmarksDeleted message) => _mqttTopicsStarter.CreateWorkersAsync(message.Bookmarks, _services);
    }
}