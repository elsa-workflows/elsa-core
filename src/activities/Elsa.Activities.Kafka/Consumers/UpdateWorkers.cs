using System.Threading.Tasks;
using Elsa.Activities.Kafka.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.Kafka.Consumers
{
    public class UpdateWorkers : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IWorkerManager _workerManager;
        public UpdateWorkers(IWorkerManager workerManager) => _workerManager = workerManager;
        public async Task Handle(TriggerIndexingFinished message) => await _workerManager.CreateWorkersAsync(message.Triggers);
        public async Task Handle(TriggersDeleted message) => await _workerManager.RemoveTagsFromWorkersAsync(new[] { message.WorkflowDefinitionId });
        public async Task Handle(BookmarkIndexingFinished message) => await _workerManager.CreateWorkersAsync(message.Bookmarks);
        public async Task Handle(BookmarksDeleted message) => await _workerManager.RemoveTagsFromWorkersAsync(new[] { message.WorkflowInstanceId });
    }
}
