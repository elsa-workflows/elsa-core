using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Web.Persistence.FileSystem.Extensions;
using OrchardCore.FileStorage;

namespace Flowsharp.Web.Persistence.FileSystem.Services
{
    public class FileSystemWorkflowStore : IWorkflowStore
    {
        private readonly IWorkflowsFileStore fileStore;
        private readonly IWorkflowSerializer workflowSerializer;
        private readonly ITokenFormatter formatter;

        public FileSystemWorkflowStore(IWorkflowsFileStore fileStore, IWorkflowSerializer workflowSerializer, ITokenFormatter formatter)
        {
            this.fileStore = fileStore;
            this.workflowSerializer = workflowSerializer;
            this.formatter = formatter;
        }

        public async Task<IEnumerable<Workflow>> GetManyAsync(ISpecification<Workflow, IWorkflowSpecificationVisitor> specification, CancellationToken cancellationToken)
        {
            var workflows = await ListAsync(cancellationToken);
            var query = workflows.AsQueryable().Where(x => specification.IsSatisfiedBy(x));
            return query.Distinct().ToList();
        }

        public async Task<Workflow> GetAsync(ISpecification<Workflow, IWorkflowSpecificationVisitor> specification, CancellationToken cancellationToken)
        {
            var workflows = await ListAsync(cancellationToken);
            var query = workflows.AsQueryable().Where(x => specification.IsSatisfiedBy(x));
            return query.Distinct().FirstOrDefault();
        }

        public Task AddAsync(Workflow value, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateAsync(Workflow value, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
        
        private async Task<IEnumerable<Workflow>> ListAsync(CancellationToken cancellationToken)
        {
            await fileStore.TryCreateDirectoryAsync("definitions");
            var files = await fileStore.GetDirectoryContentAsync("definitions");
            var loadTasks = files.Select(x => LoadWorkflowDefinitionAsync(x, cancellationToken));
            return await Task.WhenAll(loadTasks);
        }

        private async Task<Workflow> LoadWorkflowDefinitionAsync(IFileStoreEntry file, CancellationToken cancellationToken)
        {
            var data = await fileStore.ReadToEndAsync(file.Path);
            var workflow = await workflowSerializer.DeserializeAsync(data, cancellationToken);
            workflow.Metadata.Id = file.Name;
            return workflow;
        }
    }
}