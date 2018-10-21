using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Web.Persistence.Abstractions.Models;
using Flowsharp.Web.Persistence.Abstractions.Services;
using Flowsharp.Web.Persistence.FileSystem.Extensions;
using OrchardCore.FileStorage;

namespace Flowsharp.Web.Persistence.FileSystem.Services
{
    public class FileSystemWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IWorkflowsFileStore fileStore;
        private readonly IWorkflowSerializer workflowSerializer;
        private readonly ITokenFormatter formatter;

        public FileSystemWorkflowDefinitionStore(IWorkflowsFileStore fileStore, IWorkflowSerializer workflowSerializer, ITokenFormatter formatter)
        {
            this.fileStore = fileStore;
            this.workflowSerializer = workflowSerializer;
            this.formatter = formatter;
        }
        
        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken)
        {
            await fileStore.TryCreateDirectoryAsync("definitions");
            var files = await fileStore.GetDirectoryContentAsync("definitions");
            var loadTasks = files.Select(x => LoadWorkflowDefinitionAsync(x, cancellationToken));
            return await Task.WhenAll(loadTasks);
        }

        public async Task<WorkflowDefinition> GetAsync(string id, CancellationToken cancellationToken)
        {
            var path = fileStore.Combine("definitions", id);
            var file = await fileStore.GetFileInfoAsync(path);
            return await LoadWorkflowDefinitionAsync(file, cancellationToken);
        }

        private async Task<WorkflowDefinition> LoadWorkflowDefinitionAsync(IFileStoreEntry file, CancellationToken cancellationToken)
        {
            var data = await fileStore.ReadToEndAsync(file.Path);
            var token = formatter.FromString(data);
            var workflowToken = token["workflow"];
            var workflow = workflowSerializer.Deserialize(workflowToken);
            
            return new WorkflowDefinition
            {
                Id = file.Name,
                Name = token.Value<string>("name"),
                Description = token.Value<string>("description"),
                IsEnabled = token.Value<bool>("enabled"),
                Workflow = workflow
            };
        }
    }
}