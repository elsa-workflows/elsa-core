using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Core.DbContexts;
using Elsa.Persistence.Core.Entities;
using Elsa.Persistence.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.Core.Services
{
    public class EntityFrameworkCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly ElsaContext _dbContext;
        private readonly IMapper _mapper;

        public EntityFrameworkCoreWorkflowDefinitionStore(ElsaContext dbContext, IMapper mapper)
        {
            this._dbContext = dbContext;
            this._mapper = mapper;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var existingEntity =
                await _dbContext
                    .WorkflowDefinitionVersions
                    .Include(x => x.Activities)
                    .Include(x => x.Connections)
                    .FirstOrDefaultAsync(x => x.VersionId == definition.Id, cancellationToken);

            if (existingEntity == null)
                return await AddAsync(definition, cancellationToken);

            return await UpdateAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var entity = Map(definition);
            await _dbContext.WorkflowDefinitionVersions.AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var query = _dbContext
                .WorkflowDefinitionVersions
                .Include(x => x.Activities)
                .Include(x => x.Connections)
                .Where(x => x.VersionId == id);

            var entity = await query.FirstOrDefaultAsync(cancellationToken);
            
            return Map(entity);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(
            string definitionId,
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext
                .WorkflowDefinitionVersions
                .Include(x => x.Activities)
                .Include(x => x.Connections)
                .AsQueryable()
                .Where(x => x.DefinitionId == definitionId)
                .WithVersion(version);

            var entity = await query.FirstOrDefaultAsync(cancellationToken);
            
            return Map(entity);
        }
        
        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken)
        {
            var entity = await _dbContext
                .WorkflowDefinitionVersions
                .Include(x => x.Activities)
                .Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.VersionId == definition.Id, cancellationToken);

            DeleteActivities(entity);
            DeleteConnections(entity);
            
            entity = _mapper.Map(definition, entity);
            
            _dbContext.WorkflowDefinitionVersions.Update(entity);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Map(entity);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext
                .WorkflowDefinitionVersions
                .Include(x => x.Activities)
                .Include(x => x.Connections)
                .AsQueryable()
                .WithVersion(version);

            var entities = await query.ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(entities);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var definitionRecords = await _dbContext.WorkflowDefinitionVersions
                .Where(x => x.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var instanceRecords = await _dbContext
                .WorkflowInstances.Where(x => x.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var activityDefinitionRecords = await _dbContext.ActivityDefinitions
                .Where(x => x.WorkflowDefinitionVersion.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var connectionRecords = await _dbContext.ConnectionDefinitions
                .Where(x => x.WorkflowDefinitionVersion.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var activityInstanceRecords = await _dbContext.ActivityInstances
                .Where(x => x.WorkflowInstance.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var blockingActivityRecords = await _dbContext.BlockingActivities
                .Where(x => x.WorkflowInstance.DefinitionId == id)
                .ToListAsync(cancellationToken);

            _dbContext.WorkflowInstances.RemoveRange(instanceRecords);
            _dbContext.WorkflowDefinitionVersions.RemoveRange(definitionRecords);
            _dbContext.ActivityDefinitions.RemoveRange(activityDefinitionRecords);
            _dbContext.ConnectionDefinitions.RemoveRange(connectionRecords);
            _dbContext.ActivityInstances.RemoveRange(activityInstanceRecords);
            _dbContext.BlockingActivities.RemoveRange(blockingActivityRecords);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return definitionRecords.Count;
        }
        
        private void DeleteActivities(WorkflowDefinitionVersionEntity entity)
        {
            _dbContext.ActivityDefinitions.RemoveRange(entity.Activities);
            entity.Activities.Clear();
        }
        
        private void DeleteConnections(WorkflowDefinitionVersionEntity entity)
        {
            _dbContext.ConnectionDefinitions.RemoveRange(entity.Connections);
            entity.Connections.Clear();
        }

        private WorkflowDefinitionVersionEntity Map(WorkflowDefinitionVersion source) => _mapper.Map<WorkflowDefinitionVersionEntity>(source);
        private WorkflowDefinitionVersion Map(WorkflowDefinitionVersionEntity source) => _mapper.Map<WorkflowDefinitionVersion>(source);
    }
}