using System.Threading.Tasks;
using Elsa.Activities.File.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.File.Consumers
{
    public class RecreateFileSystemWatchersConsumer : IHandleMessages<WorkflowDefinitionPublished>, IHandleMessages<WorkflowDefinitionRetracted>, IHandleMessages<WorkflowDefinitionDeleted>
    {
        private readonly FileSystemWatchersStarter _fileSystemWatchersStarter;

        public RecreateFileSystemWatchersConsumer(FileSystemWatchersStarter fileSystemWatchersStarter) => _fileSystemWatchersStarter = fileSystemWatchersStarter;
        public Task Handle(WorkflowDefinitionPublished message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync();
        public Task Handle(WorkflowDefinitionRetracted message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync();
        public Task Handle(WorkflowDefinitionDeleted message) => _fileSystemWatchersStarter.CreateAndAddWatchersAsync();
    }
}