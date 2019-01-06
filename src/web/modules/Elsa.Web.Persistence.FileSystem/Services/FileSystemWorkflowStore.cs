using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Serialization.Extensions;
using Elsa.Serialization.Formatters;
using Elsa.Web.Persistence.FileSystem.Extensions;
using NodaTime;
using OrchardCore.FileStorage;

namespace Elsa.Web.Persistence.FileSystem.Services
{
    public class FileSystemWorkflowStore : IWorkflowStore
    {
        private const string Format = YamlTokenFormatter.FormatName;
        private readonly IWorkflowsFileStore fileStore;
        private readonly IWorkflowSerializer workflowSerializer;
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;

        public FileSystemWorkflowStore(
            IWorkflowsFileStore fileStore, 
            IWorkflowSerializer workflowSerializer, 
            IIdGenerator idGenerator,
            IClock clock)
        {
            this.fileStore = fileStore;
            this.workflowSerializer = workflowSerializer;
            this.idGenerator = idGenerator;
            this.clock = clock;
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

        public async Task AddAsync(Workflow value, CancellationToken cancellationToken)
        {
            var fileExtension = $".{Format.ToLower()}";
            var id = $"{idGenerator.Generate()}{fileExtension}";

            value.CreatedAt = clock.GetCurrentInstant();
            value.Metadata.Id = id;
            
            await UpdateAsync(value, cancellationToken);
        }

        public async Task UpdateAsync(Workflow value, CancellationToken cancellationToken)
        {
            var data = await workflowSerializer.SerializeAsync(value, Format, cancellationToken);
            var fileName = value.Metadata.Id;
            var path = fileName;

            using (var stream = data.ToStream())
            {
                await fileStore.CreateFileFromStream(path, stream, true);
            }
        }

        private async Task<IEnumerable<Workflow>> ListAsync(CancellationToken cancellationToken)
        {
            var files = await fileStore.GetDirectoryContentAsync();
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