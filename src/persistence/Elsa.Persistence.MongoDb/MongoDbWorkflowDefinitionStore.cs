using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb
{
    public class MongoDbWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IMongoCollection<WorkflowDefinition> _workflowDefinitions;

        public MongoDbWorkflowDefinitionStore(IMongoCollection<WorkflowDefinition> workflowDefinitions)
        {
            _workflowDefinitions = workflowDefinitions;
        }

        public async Task<int> CountAsync(VersionOptions? version = null, CancellationToken cancellationToken = default) =>
            await ((IMongoQueryable<WorkflowDefinition>) _workflowDefinitions.AsQueryable().WithVersion(version)).CountAsync(cancellationToken);

        public async Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            var filter = GetFilterWorkflowDefinitionId(workflowDefinition.Id);
            await _workflowDefinitions.DeleteOneAsync(filter, cancellationToken);
        }

        public async Task<WorkflowDefinition> GetAsync(string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default) =>
            await ((IMongoQueryable<WorkflowDefinition>) _workflowDefinitions
                .AsQueryable()
                .Where(x => x.WorkflowDefinitionId == workflowDefinitionId)
                .WithVersion(versionOptions)).FirstOrDefaultAsync(cancellationToken);

        public async Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
            await _workflowDefinitions
                .AsQueryable()
                .Where(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId)
                .FirstOrDefaultAsync(cancellationToken);
        
        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(int? skip = null, int? take = null, VersionOptions? version = null, CancellationToken cancellationToken = default)
        {
            var query = _workflowDefinitions
                .AsQueryable()
                .WithVersion(version);

            if (skip != null)
                query = query.Skip(skip.Value);

            if (take != null)
                query = query.Take(take.Value);

            return await ((IMongoQueryable<WorkflowDefinition>) query).ToListAsync(cancellationToken);
        }

        public async Task SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            if (workflowDefinition.Id == 0)
            {
                // If there is no instance yet, max throws an error
                if (await _workflowDefinitions.AsQueryable().AnyAsync(cancellationToken: cancellationToken))
                    workflowDefinition.Id = await _workflowDefinitions.AsQueryable().MaxAsync(x => x.Id, cancellationToken: cancellationToken) + 1;
                else
                    workflowDefinition.Id = 1;
            }

            var filter = GetFilterWorkflowDefinitionId(workflowDefinition.Id);

            await _workflowDefinitions.ReplaceOneAsync(filter, workflowDefinition, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }

        private static FilterDefinition<WorkflowDefinition> GetFilterWorkflowDefinitionId(int id) => Builders<WorkflowDefinition>.Filter.Where(x => x.Id == id);
    }
}