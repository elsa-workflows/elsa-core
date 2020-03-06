using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb.Services
{
    public class MongoWorkflowDefinitionVersionStore : IWorkflowDefinitionVersionStore
    {
        private readonly IMongoCollection<WorkflowDefinitionVersion> workflowDefinitionVersionCollection;
        private readonly IMongoCollection<WorkflowInstance> workflowInstanceCollection;

        public MongoWorkflowDefinitionVersionStore(
            IMongoCollection<WorkflowDefinitionVersion> workflowDefinitionVersionCollection,
            IMongoCollection<WorkflowInstance> workflowInstanceCollection)
        {
            this.workflowDefinitionVersionCollection = workflowDefinitionVersionCollection;
            this.workflowInstanceCollection = workflowInstanceCollection;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(
            WorkflowDefinitionVersion definitionVersion,
            CancellationToken cancellationToken = default)
        {
            await workflowDefinitionVersionCollection.ReplaceOneAsync(
                x => x.Id == definitionVersion.Id && x.Version == definitionVersion.Version,
                definitionVersion,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken
            );

            return definitionVersion;
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            await workflowDefinitionVersionCollection.InsertOneAsync(definitionVersion, new InsertOneOptions(), cancellationToken);
            return definitionVersion;
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return workflowDefinitionVersionCollection.AsQueryable().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(
            string definitionId,
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = (IMongoQueryable<WorkflowDefinitionVersion>)workflowDefinitionVersionCollection.AsQueryable()
                .Where(x => x.DefinitionId == definitionId)
                .WithVersion(version);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = workflowDefinitionVersionCollection.AsQueryable();
            var results = await query.ToListAsync(cancellationToken);
            return results.WithVersion(version);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definitionVersion,
            CancellationToken cancellationToken)
        {
            await workflowDefinitionVersionCollection.ReplaceOneAsync(
                x => x.Id == definitionVersion.Id && x.Version == definitionVersion.Version,
                definitionVersion,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken
            );

            return definitionVersion;
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            await workflowInstanceCollection.DeleteManyAsync(x => x.DefinitionId == id, cancellationToken);
            var result = await workflowDefinitionVersionCollection.DeleteManyAsync(x => x.DefinitionId == id, cancellationToken);

            return (int)result.DeletedCount;
        }
    }
}