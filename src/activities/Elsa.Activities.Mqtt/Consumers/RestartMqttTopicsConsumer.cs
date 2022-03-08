using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Services;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.Mqtt.Consumers
{
    public class RestartMqttTopicsConsumer : INotificationHandler<TriggerIndexingFinished>, INotificationHandler<TriggersDeleted>, INotificationHandler<BookmarkIndexingFinished>, INotificationHandler<BookmarksDeleted>
    {
        private readonly IMqttTopicsStarter _mqttTopicsStarter;
        private readonly IServiceProvider _services;

        public RestartMqttTopicsConsumer(IMqttTopicsStarter mqttTopicsStarter, IServiceProvider services)
        {
            _mqttTopicsStarter = mqttTopicsStarter;
            _services = services;
        }

        public Task Handle(TriggerIndexingFinished message, CancellationToken cancellationToken) => _mqttTopicsStarter.CreateWorkersAsync(message.Triggers, _services);
        public Task Handle(TriggersDeleted message, CancellationToken cancellationToken) => _mqttTopicsStarter.RemoveWorkersAsync(message.Triggers);
        public Task Handle(BookmarkIndexingFinished message, CancellationToken cancellationToken) => _mqttTopicsStarter.CreateWorkersAsync(message.Bookmarks, _services);
        public Task Handle(BookmarksDeleted message, CancellationToken cancellationToken) => _mqttTopicsStarter.CreateWorkersAsync(message.Bookmarks, _services);
    }
}