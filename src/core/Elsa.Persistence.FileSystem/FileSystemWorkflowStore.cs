using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.FileSystem.Options;
using Elsa.Serialization;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Elsa.Persistence.FileSystem
{
    public class FileSystemWorkflowStore : IWorkflowStore
    {
        private readonly IFileSystem fileSystem;
        private readonly IIdGenerator idGenerator;
        private readonly IWorkflowSerializer workflowSerializer;
        private readonly IClock clock;
        private readonly string rootDirectory;
        private readonly string format;

        public FileSystemWorkflowStore(
            IOptions<FileSystemStoreOptions> options,
            IFileSystem fileSystem,
            IIdGenerator idGenerator,
            IWorkflowSerializer workflowSerializer,
            IClock clock)
        {
            this.fileSystem = fileSystem;
            this.idGenerator = idGenerator;
            this.workflowSerializer = workflowSerializer;
            this.clock = clock;
            rootDirectory = options.Value.RootDirectory;
            format = options.Value.Format;
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

        public async Task SaveAsync(Workflow value, CancellationToken cancellationToken)
        {
            if(!fileSystem.Path.HasExtension(value.Metadata.Id))
                await AddAsync(value, cancellationToken);
            else
                await UpdateAsync(value, cancellationToken);
        }

        public async Task AddAsync(Workflow value, CancellationToken cancellationToken)
        {
            var id = string.IsNullOrWhiteSpace(value.Metadata.Id) ? idGenerator.Generate() : value.Metadata.Id;

            if (!fileSystem.Path.HasExtension(id))
                id = $"{id}.{format.ToLower()}";

            value.Metadata.Id = id;
            value.CreatedAt = clock.GetCurrentInstant();

            await UpdateAsync(value, cancellationToken);
        }

        public async Task UpdateAsync(Workflow value, CancellationToken cancellationToken)
        {
            EnsureWorkflowsDirectoryAsync();
            var data = await workflowSerializer.SerializeAsync(value, format, cancellationToken);
            var fileName = value.Metadata.Id;
            var path = fileSystem.Path.Combine(rootDirectory, fileName);

            fileSystem.File.WriteAllText(path, data);
        }

        private async Task<IEnumerable<Workflow>> ListAsync(CancellationToken cancellationToken)
        {
            EnsureWorkflowsDirectoryAsync();
            var files = fileSystem.Directory.GetFiles(rootDirectory);
            var loadTasks = files.Select(x => LoadWorkflowDefinitionAsync(x, cancellationToken));
            return await Task.WhenAll(loadTasks);
        }

        private async Task<Workflow> LoadWorkflowDefinitionAsync(string path, CancellationToken cancellationToken)
        {
            var data = fileSystem.File.ReadAllText(path);
            var workflow = await workflowSerializer.DeserializeAsync(data, format, cancellationToken);
            workflow.Metadata.Id = fileSystem.Path.GetFileName(path);
            return workflow;
        }

        private void EnsureWorkflowsDirectoryAsync()
        {
            fileSystem.Directory.CreateDirectory(rootDirectory);
        }
    }
}