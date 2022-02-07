using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.AzureServiceBus.Consumers
{
    public class RestartServiceBusTopicsConsumer : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IServiceBusTopicsStarter _serviceBusTopicsStarter;
        public RestartServiceBusTopicsConsumer(IServiceBusTopicsStarter serviceBusTopicsStarter) => _serviceBusTopicsStarter = serviceBusTopicsStarter;
        public async Task Handle(TriggerIndexingFinished message) => await _serviceBusTopicsStarter.CreateWorkersAsync(message.Triggers);
        public async Task Handle(TriggersDeleted message) => await _serviceBusTopicsStarter.RemoveWorkersAsync(message.Triggers);
        public async Task Handle(BookmarkIndexingFinished message) => await _serviceBusTopicsStarter.CreateWorkersAsync(message.Bookmarks);
        public async Task Handle(BookmarksDeleted message) => await _serviceBusTopicsStarter.CreateWorkersAsync(message.Bookmarks);
    }
}