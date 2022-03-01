using System;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.RabbitMq.Consumers
{
    public class RestartRabbitMqBusConsumer : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        private readonly IServiceProvider _services;

        public RestartRabbitMqBusConsumer(IRabbitMqQueueStarter rabbitMqQueueStarter, IServiceProvider services)
        {
            _rabbitMqQueueStarter = rabbitMqQueueStarter;
            _services = services;
        }
        public Task Handle(TriggerIndexingFinished message) => _rabbitMqQueueStarter.CreateWorkersAsync(message.Triggers, _services);
        public Task Handle(TriggersDeleted message) => _rabbitMqQueueStarter.RemoveWorkersAsync(message.Triggers);
        public Task Handle(BookmarkIndexingFinished message) => _rabbitMqQueueStarter.CreateWorkersAsync(message.Bookmarks, _services);
        public Task Handle(BookmarksDeleted message) => _rabbitMqQueueStarter.CreateWorkersAsync(message.Bookmarks, _services);
    }
}
