using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.MongoDb.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb.Services
{
    public class MongoWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IMongoCollection<WorkflowDefinition> collection;

        public MongoWorkflowDefinitionStore(IMongoCollection<WorkflowDefinition> collection)
        {
            this.collection = collection;
        }

        public async Task<WorkflowDefinition> SaveAsync(
            WorkflowDefinition definition,
            CancellationToken cancellationToken = default)
        {
            await collection.ReplaceOneAsync(
                x => x.Id == definition.Id && x.Version == definition.Version,
                definition,
                new UpdateOptions { IsUpsert = true },
                cancellationToken
            );

            return definition;
        }

        public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            await collection.InsertOneAsync(definition, new InsertOneOptions(), cancellationToken);
        }

        public async Task<WorkflowDefinition> GetByIdAsync(
            string id, 
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = (IMongoQueryable<WorkflowDefinition>)collection.AsQueryable()
                .Where(x => x.Id == id)
                .WithVersion(version);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = (IMongoQueryable<WorkflowDefinition>)collection.AsQueryable().WithVersion(version);
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition definition,
            CancellationToken cancellationToken)
        {
            await collection.ReplaceOneAsync(
                x => x.Id == definition.Id && x.Version == definition.Version,
                definition,
                new UpdateOptions { IsUpsert = false },
                cancellationToken
            );

            return definition;
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await collection.DeleteManyAsync(x => x.Id == id, cancellationToken);

            return (int) result.DeletedCount;
        }
    }
}