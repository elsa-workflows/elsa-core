using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;
using Elsa.Repositories;
using Elsa.Services;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb
{
    public class MongoDbWorkflowDefinitionRepository : IWorkflowDefinitionRepository
    {
        private readonly IMongoCollection<WorkflowDefinition> _workflowDefinitions;
        private readonly IIdGenerator _idGenerator;


        public MongoDbWorkflowDefinitionRepository(IMongoCollection<WorkflowDefinition> workflowDefinitions, IIdGenerator idGenerator)
        {
            _workflowDefinitions = workflowDefinitions;
            _idGenerator = idGenerator;
        }

        public async Task<int> CountAsync(VersionOptions? version = null, CancellationToken cancellationToken = default)
        {
            return await ((IMongoQueryable<WorkflowDefinition>)_workflowDefinitions.AsQueryable().WithVersion(version)).CountAsync();
        }

        public async Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            var filter = GetFilterWorkflowDefinitionId(workflowDefinition.Id);
            await _workflowDefinitions.DeleteOneAsync(filter);
        }

        public async Task<WorkflowDefinition> GetAsync(string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
        {
            return await((IMongoQueryable<WorkflowDefinition>)_workflowDefinitions
                .AsQueryable()
                .Where(x => x.WorkflowDefinitionId == workflowDefinitionId)
                .WithVersion(versionOptions)).FirstOrDefaultAsync();
        }

        public async Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
        {
            return await((IMongoQueryable<WorkflowDefinition>)_workflowDefinitions
                 .AsQueryable()
                 .Where(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId)).FirstOrDefaultAsync();
        }

        public WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition)
        {
            if (string.IsNullOrWhiteSpace(workflowDefinition.WorkflowDefinitionId))
                workflowDefinition.WorkflowDefinitionId = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (string.IsNullOrWhiteSpace(workflowDefinition.WorkflowDefinitionVersionId))
                workflowDefinition.WorkflowDefinitionVersionId = _idGenerator.Generate();

            return workflowDefinition;
        }

        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(int? skip = null, int? take = null, VersionOptions? version = null, CancellationToken cancellationToken = default)
        {
            var query = _workflowDefinitions
               .AsQueryable()
               .WithVersion(version);

            if (skip != null)
                query = query.Skip(skip.Value);

            if (take != null)
                query = query.Take(take.Value);

            return await ((IMongoQueryable<WorkflowDefinition>)query).ToListAsync();
        }

        public async Task SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            if (workflowDefinition.Id == 0)
            {
                // If there is no instance yet, max throws an error
                if (await _workflowDefinitions.AsQueryable().AnyAsync())
                {
                    workflowDefinition.Id = await _workflowDefinitions.AsQueryable().MaxAsync(x => x.Id) + 1;
                }
                else
                {
                    workflowDefinition.Id = 1;
                }
            }

            var filter = GetFilterWorkflowDefinitionId(workflowDefinition.Id);

            await _workflowDefinitions.ReplaceOneAsync(filter, workflowDefinition, new ReplaceOptions { IsUpsert = true });
        }

        private FilterDefinition<WorkflowDefinition> GetFilterWorkflowDefinitionId(int id) => Builders<WorkflowDefinition>.Filter.Where(x => x.Id == id);
    }
}
