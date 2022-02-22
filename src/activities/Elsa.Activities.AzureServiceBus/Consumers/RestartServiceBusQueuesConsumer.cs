using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.AzureServiceBus.Consumers
{
    public class RestartServiceBusQueuesConsumer : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IServiceBusQueuesStarter _serviceBusQueuesStarter;
        public RestartServiceBusQueuesConsumer(IServiceBusQueuesStarter serviceBusQueuesStarter) => _serviceBusQueuesStarter = serviceBusQueuesStarter;
        public async Task Handle(TriggerIndexingFinished message) => await _serviceBusQueuesStarter.CreateWorkersAsync(message.Triggers);
        public async Task Handle(TriggersDeleted message) => await _serviceBusQueuesStarter.RemoveWorkersAsync(message.Triggers);
        public async Task Handle(BookmarkIndexingFinished message) => await _serviceBusQueuesStarter.CreateWorkersAsync(message.Bookmarks);
        public async Task Handle(BookmarksDeleted message) => await _serviceBusQueuesStarter.RemoveWorkersAsync(message.Bookmarks);
    }
}