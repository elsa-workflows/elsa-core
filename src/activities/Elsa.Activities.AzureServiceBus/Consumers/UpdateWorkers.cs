using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.AzureServiceBus.Consumers
{
    public class UpdateWorkers : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IWorkersStarter _workersStarter;
        public UpdateWorkers(IWorkersStarter workersStarter) => _workersStarter = workersStarter;
        public async Task Handle(TriggerIndexingFinished message) => await _workersStarter.CreateWorkersAsync(message.Triggers);
        public async Task Handle(TriggersDeleted message) => await _workersStarter.RemoveWorkersAsync(message.Triggers);
        public async Task Handle(BookmarkIndexingFinished message) => await _workersStarter.CreateWorkersAsync(message.Bookmarks);
        public async Task Handle(BookmarksDeleted message) => await _workersStarter.CreateWorkersAsync(message.Bookmarks);
    }
}