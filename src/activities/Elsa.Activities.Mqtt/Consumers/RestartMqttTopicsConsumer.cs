using System;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Services;
using Elsa.Events;
using Elsa.MultiTenancy;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Elsa.Activities.Mqtt.Consumers
{
    public class RestartMqttTopicsConsumer : MultitenantConsumer, IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IMqttTopicsStarter _mqttTopicsStarter;

        public RestartMqttTopicsConsumer(IMqttTopicsStarter mqttTopicsStarter, IMessageContext messageContext, IServiceProvider serviceProvider) : base(messageContext, serviceProvider) => _mqttTopicsStarter = mqttTopicsStarter;

        public async Task Handle(TriggerIndexingFinished message) => await _mqttTopicsStarter.CreateWorkersAsync(message.Triggers, _serviceProvider);
        public async Task Handle(TriggersDeleted message) => await _mqttTopicsStarter.RemoveWorkersAsync(message.Triggers);
        public async Task Handle(BookmarkIndexingFinished message) => await _mqttTopicsStarter.CreateWorkersAsync(message.Bookmarks, _serviceProvider);
        public async Task Handle(BookmarksDeleted message) => await _mqttTopicsStarter.CreateWorkersAsync(message.Bookmarks, _serviceProvider);

    }
}