using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Persistence;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Extensions;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Web.Persistence.FileSystem.Extensions;
using OrchardCore.FileStorage;

namespace Flowsharp.Web.Persistence.FileSystem.Services
{
    public class FileSystemWorkflowStore : IWorkflowStore
    {
        private readonly IWorkflowsFileStore fileStore;
        private readonly IWorkflowSerializer workflowSerializer;
        private const string Format = YamlTokenFormatter.FormatName;

        public FileSystemWorkflowStore(IWorkflowsFileStore fileStore, IWorkflowSerializer workflowSerializer)
        {
            this.fileStore = fileStore;
            this.workflowSerializer = workflowSerializer;
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

        public async Task UpdateAsync(Workflow value, CancellationToken cancellationToken)
        {
            await fileStore.TryCreateDirectoryAsync("workflows");
            var data = await workflowSerializer.SerializeAsync(value, Format, cancellationToken);
            var path = fileStore.Combine("workflows", value.Metadata.Id);

            using (var stream = data.ToStream())
            {
                await fileStore.CreateFileFromStream(path, stream, true);
            }
        }
        
        private async Task<IEnumerable<Workflow>> ListAsync(CancellationToken cancellationToken)
        {
            await fileStore.TryCreateDirectoryAsync("workflows");
            var files = await fileStore.GetDirectoryContentAsync("workflows");
            var loadTasks = files.Select(x => LoadWorkflowDefinitionAsync(x, cancellationToken));
            return await Task.WhenAll(loadTasks);
        }

        private async Task<Workflow> LoadWorkflowDefinitionAsync(IFileStoreEntry file, CancellationToken cancellationToken)
        {
            var data = await fileStore.ReadToEndAsync(file.Path);
            var workflow = await workflowSerializer.DeserializeAsync(data, Format, cancellationToken);
            workflow.Metadata.Id = file.Name;
            return workflow;
        }
    }
}