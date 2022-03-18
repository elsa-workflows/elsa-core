using Elsa.Activities.RabbitMq.Services;
using Elsa.Events;
using Rebus.Handlers;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Consumers
{
    public class RestartRabbitMqBusConsumer : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        public RestartRabbitMqBusConsumer(IRabbitMqQueueStarter rabbitMqQueueStarter) => _rabbitMqQueueStarter = rabbitMqQueueStarter;
        public Task Handle(TriggerIndexingFinished message) => _rabbitMqQueueStarter.CreateWorkersAsync();
        public Task Handle(TriggersDeleted message) => _rabbitMqQueueStarter.RemoveWorkersAsync(message.Triggers);
        public Task Handle(BookmarkIndexingFinished message) => _rabbitMqQueueStarter.CreateWorkersAsync();
        public async Task Handle(BookmarksDeleted message) => await _rabbitMqQueueStarter.RemoveWorkersAsync(message.Bookmarks);
    }
}
