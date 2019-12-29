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
        private readonly IMongoCollection<ProcessDefinitionVersion> workflowDefinitionCollection;
        private readonly IMongoCollection<ProcessInstance> workflowInstanceCollection;

        public MongoWorkflowDefinitionStore(
            IMongoCollection<ProcessDefinitionVersion> workflowDefinitionCollection,
            IMongoCollection<ProcessInstance> workflowInstanceCollection)
        {
            this.workflowDefinitionCollection = workflowDefinitionCollection;
            this.workflowInstanceCollection = workflowInstanceCollection;
        }

        public async Task<ProcessDefinitionVersion> SaveAsync(
            ProcessDefinitionVersion definition,
            CancellationToken cancellationToken = default)
        {
            await workflowDefinitionCollection.ReplaceOneAsync(
                x => x.Id == definition.Id && x.Version == definition.Version,
                definition,
                new UpdateOptions { IsUpsert = true },
                cancellationToken
            );

            return definition;
        }

        public async Task<ProcessDefinitionVersion> AddAsync(ProcessDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            await workflowDefinitionCollection.InsertOneAsync(definition, new InsertOneOptions(), cancellationToken);
            return definition;
        }

        public Task<ProcessDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return workflowDefinitionCollection.AsQueryable().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<ProcessDefinitionVersion> GetByIdAsync(
            string definitionId, 
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = (IMongoQueryable<ProcessDefinitionVersion>)workflowDefinitionCollection.AsQueryable()
                .Where(x => x.DefinitionId == definitionId)
                .WithVersion(version);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProcessDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = workflowDefinitionCollection.AsQueryable();
            var results = await query.ToListAsync(cancellationToken);
            return results.WithVersion(version);
        }

        public async Task<ProcessDefinitionVersion> UpdateAsync(ProcessDefinitionVersion definition,
            CancellationToken cancellationToken)
        {
            await workflowDefinitionCollection.ReplaceOneAsync(
                x => x.Id == definition.Id && x.Version == definition.Version,
                definition,
                new UpdateOptions { IsUpsert = false },
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