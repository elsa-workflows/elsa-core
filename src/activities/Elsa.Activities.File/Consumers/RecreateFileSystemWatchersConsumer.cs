using System.Threading.Tasks;
using Elsa.Activities.File.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.File.Consumers
{
    public class RecreateFileSystemWatchersConsumer : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly FileSystemWatchersStarter _fileSystemWatchersStarter;
        public RecreateFileSystemWatchersConsumer(FileSystemWatchersStarter fileSystemWatchersStarter) => _fileSystemWatchersStarter = fileSystemWatchersStarter;
        public Task Handle(TriggerIndexingFinished message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync();
        public Task Handle(TriggersDeleted message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync();
        public Task Handle(BookmarkIndexingFinished message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync();
        public Task Handle(BookmarksDeleted message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync();
    }
}