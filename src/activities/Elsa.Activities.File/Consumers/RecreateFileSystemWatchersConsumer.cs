using System;
using System.Threading.Tasks;
using Elsa.Activities.File.Services;
using Elsa.Events;
using Elsa.MultiTenancy;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Elsa.Activities.File.Consumers
{
    public class RecreateFileSystemWatchersConsumer : MultitenantConsumer, IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly FileSystemWatchersStarter _fileSystemWatchersStarter;
        public RecreateFileSystemWatchersConsumer(FileSystemWatchersStarter fileSystemWatchersStarter, IMessageContext messageContext, IServiceProvider serviceProvider) : base(messageContext, serviceProvider) => _fileSystemWatchersStarter = fileSystemWatchersStarter;
        public Task Handle(TriggerIndexingFinished message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync(ServiceProvider);
        public Task Handle(TriggersDeleted message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync(ServiceProvider);
        public Task Handle(BookmarkIndexingFinished message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync(ServiceProvider);
        public Task Handle(BookmarksDeleted message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync(ServiceProvider);
    }
}