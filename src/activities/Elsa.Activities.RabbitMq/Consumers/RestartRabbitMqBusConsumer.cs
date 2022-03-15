using System;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Events;
using Elsa.MultiTenancy;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Elsa.Activities.RabbitMq.Consumers
{
    public class RestartRabbitMqBusConsumer : MultitenantConsumer, IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;

        public RestartRabbitMqBusConsumer(IRabbitMqQueueStarter rabbitMqQueueStarter, IMessageContext messageContext, IServiceProvider serviceProvider) : base(messageContext, serviceProvider) => _rabbitMqQueueStarter = rabbitMqQueueStarter;

        public async Task Handle(TriggerIndexingFinished message) => await _rabbitMqQueueStarter.CreateWorkersAsync(message.Triggers, _serviceProvider);
        public async Task Handle(TriggersDeleted message) => await _rabbitMqQueueStarter.RemoveWorkersAsync(message.Triggers);
        public async Task Handle(BookmarkIndexingFinished message) => await _rabbitMqQueueStarter.CreateWorkersAsync(message.Bookmarks, _serviceProvider);
        public async Task Handle(BookmarksDeleted message) => await _rabbitMqQueueStarter.CreateWorkersAsync(message.Bookmarks, _serviceProvider);
    }
}
