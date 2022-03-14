using Elsa.Activities.OpcUa.Services;
using Elsa.Events;
using Rebus.Handlers;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Consumers
{
    public class RestartOpcUaBusConsumer : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IOpcUaQueueStarter _OpcUaQueueStarter;
        public RestartOpcUaBusConsumer(IOpcUaQueueStarter OpcUaQueueStarter) => _OpcUaQueueStarter = OpcUaQueueStarter;
        public Task Handle(TriggerIndexingFinished message) => _OpcUaQueueStarter.CreateWorkersAsync();
        public Task Handle(TriggersDeleted message) => _OpcUaQueueStarter.CreateWorkersAsync();
        public Task Handle(BookmarkIndexingFinished message) => _OpcUaQueueStarter.CreateWorkersAsync();
        public Task Handle(BookmarksDeleted message) => _OpcUaQueueStarter.CreateWorkersAsync();
    }
}
