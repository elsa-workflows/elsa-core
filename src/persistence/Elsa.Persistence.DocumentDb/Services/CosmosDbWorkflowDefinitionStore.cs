using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Extensions;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Services;

namespace Elsa.Persistence.DocumentDb.Services
{
    public class CosmosDbWorkflowDefinitionStore : CosmosDbWorkflowStoreBase<WorkflowDefinitionVersion, WorkflowDefinitionVersionDocument>, IWorkflowDefinitionStore
    {
        public CosmosDbWorkflowDefinitionStore(IMapper mapper, ICosmosDbStoreHelper<WorkflowDefinitionVersionDocument> cosmosDbStoreHelper) 
            : base(mapper, cosmosDbStoreHelper) { }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            document = await cosmosDbStoreHelper.AddAsync(document, cancellationToken);
            return Map(document);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var documents = await cosmosDbStoreHelper.ListAsync(q => q.Where(c => c.DefinitionId == id), cancellationToken);
            var tasks = documents.Select(d => cosmosDbStoreHelper.DeleteAsync(d, cancellationToken)).ToArray();
            await Task.WhenAll(tasks);

            return documents.Count;
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var document = await cosmosDbStoreHelper.FirstOrDefaultAsync(q =>
            {
                return q.Where(c => c.DefinitionId == id)
                    .WithVersion(version);
            });
            return Map(document);
        }
        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var documents = await cosmosDbStoreHelper.ListAsync(q => q.WithVersion(version), cancellationToken);
            return Map(documents);
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var document = Map(definition);
            document = await cosmosDbStoreHelper.SaveAsync(document, cancellationToken);
            return Map(document);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default) => 
            await SaveAsync(definition, cancellationToken);
    }
}
