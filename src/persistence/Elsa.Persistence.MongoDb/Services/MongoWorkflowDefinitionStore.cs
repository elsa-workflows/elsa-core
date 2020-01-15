using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb.Services
{
    public class MongoWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IMongoCollection<WorkflowDefinitionVersion> workflowDefinitionCollection;
        private readonly IMongoCollection<WorkflowInstance> workflowInstanceCollection;

        public MongoWorkflowDefinitionStore(
            IMongoCollection<WorkflowDefinitionVersion> workflowDefinitionCollection,
            IMongoCollection<WorkflowInstance> workflowInstanceCollection)
        {
            this.workflowDefinitionCollection = workflowDefinitionCollection;
            this.workflowInstanceCollection = workflowInstanceCollection;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(
            WorkflowDefinitionVersion definition,
            CancellationToken cancellationToken = default)
        {
            await workflowDefinitionCollection.ReplaceOneAsync(
                x => x.Id == definition.Id && x.Version == definition.Version,
                definition,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken
            );

            return definition;
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            await workflowDefinitionCollection.InsertOneAsync(definition, new InsertOneOptions(), cancellationToken);
            return definition;
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(
            string id, 
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = (IMongoQueryable<WorkflowDefinitionVersion>)workflowDefinitionCollection.AsQueryable()
                .Where(x => x.DefinitionId == id)
                .WithVersion(version);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = workflowDefinitionCollection.AsQueryable();
            var results = await query.ToListAsync(cancellationToken);
            return results.WithVersion(version);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition,
            CancellationToken cancellationToken)
        {
            await workflowDefinitionCollection.ReplaceOneAsync(
                x => x.Id == definition.Id && x.Version == definition.Version,
                definition,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken
            );

            return definition;
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            await workflowInstanceCollection.DeleteManyAsync(x => x.DefinitionId == id, cancellationToken);
            var result = await workflowDefinitionCollection.DeleteManyAsync(x => x.DefinitionId == id, cancellationToken);

            return (int) result.DeletedCount;
        }
    }
}