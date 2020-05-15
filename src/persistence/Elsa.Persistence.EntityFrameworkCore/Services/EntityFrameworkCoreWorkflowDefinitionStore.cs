using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Services
{
    public class EntityFrameworkCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly ElsaContext dbContext;
        private readonly IMapper mapper;

        public EntityFrameworkCoreWorkflowDefinitionStore(ElsaContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var existingEntity =
                await dbContext
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
            await dbContext.WorkflowDefinitionVersions.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken)
        {
            var entity = await dbContext
                .WorkflowDefinitionVersions
                .Include(x => x.Activities)
                .Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.VersionId == definition.Id, cancellationToken);

            DeleteActivities(entity);
            DeleteConnections(entity);
            
            entity = mapper.Map(definition, entity);
            
            dbContext.WorkflowDefinitionVersions.Update(entity);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(entity);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext
                .WorkflowDefinitionVersions
                .Include(x => x.Activities)
                .Include(x => x.Connections)
                .AsQueryable()
                .Where(x => x.DefinitionId == id)
                .WithVersion(version);

            var entity = await query.FirstOrDefaultAsync(cancellationToken);
            
            return Map(entity);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext
                .WorkflowDefinitionVersions
                .Include(x => x.Activities)
                .Include(x => x.Connections)
                .AsQueryable()
                .WithVersion(version);

            var entities = await query.ToListAsync(cancellationToken);

            return mapper.Map<IEnumerable<WorkflowDefinitionVersion>>(entities);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var definitionRecords = await dbContext.WorkflowDefinitionVersions
                .Where(x => x.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var instanceRecords = await dbContext
                .WorkflowInstances.Where(x => x.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var activityDefinitionRecords = await dbContext.ActivityDefinitions
                .Where(x => x.WorkflowDefinitionVersion.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var connectionRecords = await dbContext.ConnectionDefinitions
                .Where(x => x.WorkflowDefinitionVersion.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var activityInstanceRecords = await dbContext.ActivityInstances
                .Where(x => x.WorkflowInstance.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var blockingActivityRecords = await dbContext.BlockingActivities
                .Where(x => x.WorkflowInstance.DefinitionId == id)
                .ToListAsync(cancellationToken);

            dbContext.WorkflowInstances.RemoveRange(instanceRecords);
            dbContext.WorkflowDefinitionVersions.RemoveRange(definitionRecords);
            dbContext.ActivityDefinitions.RemoveRange(activityDefinitionRecords);
            dbContext.ConnectionDefinitions.RemoveRange(connectionRecords);
            dbContext.ActivityInstances.RemoveRange(activityInstanceRecords);
            dbContext.BlockingActivities.RemoveRange(blockingActivityRecords);

            await dbContext.SaveChangesAsync(cancellationToken);

            return definitionRecords.Count;
        }
        
        private void DeleteActivities(WorkflowDefinitionVersionEntity entity)
        {
            dbContext.ActivityDefinitions.RemoveRange(entity.Activities);
            entity.Activities.Clear();
        }
        
        private void DeleteConnections(WorkflowDefinitionVersionEntity entity)
        {
            dbContext.ConnectionDefinitions.RemoveRange(entity.Connections);
            entity.Connections.Clear();
        }

        private WorkflowDefinitionVersionEntity Map(WorkflowDefinitionVersion source) => mapper.Map<WorkflowDefinitionVersionEntity>(source);
        private WorkflowDefinitionVersion Map(WorkflowDefinitionVersionEntity source) => mapper.Map<WorkflowDefinitionVersion>(source);
    }
}