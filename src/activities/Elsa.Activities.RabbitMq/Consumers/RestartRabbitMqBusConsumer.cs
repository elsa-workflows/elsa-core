using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.RabbitMq.Consumers
{
    public class RestartRabbitMqBusConsumer : INotificationHandler<TriggerIndexingFinished>, INotificationHandler<TriggersDeleted>, INotificationHandler<BookmarkIndexingFinished>, INotificationHandler<BookmarksDeleted>
    {
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        private readonly IServiceProvider _services;

        public RestartRabbitMqBusConsumer(IRabbitMqQueueStarter rabbitMqQueueStarter, IServiceProvider services)
        {
            _rabbitMqQueueStarter = rabbitMqQueueStarter;
            _services = services;

        }

        public Task Handle(TriggerIndexingFinished message, CancellationToken cancellationToken) => _rabbitMqQueueStarter.CreateWorkersAsync(message.Triggers, _services);
        public Task Handle(TriggersDeleted message, CancellationToken cancellationToken) => _rabbitMqQueueStarter.RemoveWorkersAsync(message.Triggers);
        public Task Handle(BookmarkIndexingFinished message, CancellationToken cancellationToken) => _rabbitMqQueueStarter.CreateWorkersAsync(message.Bookmarks, _services);
        public Task Handle(BookmarksDeleted message, CancellationToken cancellationToken) => _rabbitMqQueueStarter.CreateWorkersAsync(message.Bookmarks, _services);
    }
}
