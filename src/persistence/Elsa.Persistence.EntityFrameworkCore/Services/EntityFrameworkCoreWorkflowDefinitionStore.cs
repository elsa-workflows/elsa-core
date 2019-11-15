using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.CompilerServices;

namespace Elsa.Persistence.EntityFrameworkCore.Services
{
    public class EntityFrameworkCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly ElsaContext dbContext;
        private readonly IMapper mapper;
        private readonly IIdGenerator idGenerator;

        public EntityFrameworkCoreWorkflowDefinitionStore(ElsaContext dbContext, IMapper mapper,
            IIdGenerator idGenerator)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.idGenerator = idGenerator;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(
            WorkflowDefinitionVersion definition,
            CancellationToken cancellationToken = default)
        {
            var existingEntity =
                await dbContext.WorkflowDefinitionVersions.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);

            if (existingEntity == null)
                return await AddAsync(definition, cancellationToken);

            return await UpdateAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var entity = Map(definition);
            entity.Activities = Map(definition.Activities).ToList();
            
            await dbContext.WorkflowDefinitionVersions.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }
        
        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken)
        {
            var entity = await dbContext.WorkflowDefinitionVersions.FindAsync(definition.Id);

            DeleteActivities(entity);
            entity = mapper.Map(definition, entity);
            entity.Activities = Map(definition.Activities).ToList();
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

            return Map(entities);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var definitionRecords = await dbContext.WorkflowDefinitionVersions
                .Where(x => x.DefinitionId == id)
                .ToListAsync(cancellationToken);

            var instanceRecords = await dbContext
                .WorkflowInstances.Where(x => x.DefinitionId == id)
                .ToListAsync(cancellationToken);

            dbContext.WorkflowInstances.RemoveRange(instanceRecords);
            dbContext.WorkflowDefinitionVersions.RemoveRange(definitionRecords);

            await dbContext.SaveChangesAsync(cancellationToken);

            return definitionRecords.Count;
        }
        
        private void DeleteActivities(WorkflowDefinitionVersionEntity entity)
        {
            dbContext.ActivityDefinitions.RemoveRange(entity.Activities);
            entity.Activities.Clear();
        }

        private WorkflowDefinitionVersionEntity Map(WorkflowDefinitionVersion source)
        {
            var entity = mapper.Map<WorkflowDefinitionVersionEntity>(source);

            foreach (var activity in entity.Activities)
                activity.WorkflowDefinitionVersion = entity;

            return entity;
        }

        private WorkflowDefinitionVersion Map(WorkflowDefinitionVersionEntity source)
        {
            return mapper.Map<WorkflowDefinitionVersion>(source);
        }

        private IEnumerable<WorkflowDefinitionVersion> Map(IEnumerable<WorkflowDefinitionVersionEntity> source)
        {
            return source.Select(Map);
        }
        
        private IEnumerable<ActivityDefinitionEntity> Map(IEnumerable<ActivityDefinition> activities)
        {
            foreach (var activity in activities)
            {
                var entity = mapper.Map<ActivityDefinitionEntity>(activity);
                entity.Id = idGenerator.Generate();
                yield return entity;
            }
        }
        
//        private IEnumerable<ConnectionDefinitionEntity> Map(IEnumerable<ConnectionDefinition> connections)
//        {
//            foreach (var connection in connections)
//            {
//                var entity = mapper.Map<ConnectionDefinitionEntity>(connection);
//                entity.Id = idGenerator.Generate();
//                yield return entity;
//            }
//        }
    }
}