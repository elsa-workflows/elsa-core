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
        private readonly IMongoCollection<WorkflowDefinitionVersion> collection;

        public MongoWorkflowDefinitionStore(IMongoCollection<WorkflowDefinitionVersion> collection)
        {
            this.collection = collection;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(
            WorkflowDefinitionVersion definition,
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

        public async Task AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            await collection.InsertOneAsync(definition, new InsertOneOptions(), cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(
            string id, 
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = (IMongoQueryable<WorkflowDefinitionVersion>)collection.AsQueryable()
                .Where(x => x.DefinitionId == id)
                .WithVersion(version);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = (IMongoQueryable<WorkflowDefinitionVersion>)collection.AsQueryable().WithVersion(version);
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition,
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
            var result = await collection.DeleteManyAsync(x => x.DefinitionId == id, cancellationToken);

            return (int) result.DeletedCount;
        }
    }
}