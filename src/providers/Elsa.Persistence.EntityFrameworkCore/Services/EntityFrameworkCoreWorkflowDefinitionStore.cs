using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task<WorkflowDefinition> SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var existingEntity =
                await dbContext
                    .WorkflowDefinitions
                    .FirstOrDefaultAsync(x => x.TenantId == definition.TenantId && x.Id == definition.Id, cancellationToken);

            if (existingEntity == null)
                return await AddAsync(definition, cancellationToken);

            return await UpdateAsync(definition, cancellationToken);
        }
        public async Task<WorkflowDefinition> AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var entity = Map(definition);
            await dbContext.WorkflowDefinitions.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }

        public async Task<WorkflowDefinition> GetByIdAsync(string tenantId, string id, CancellationToken cancellationToken = default)
        {
            var query = dbContext
                .WorkflowDefinitions
                .Include(x => x.WorkflowDefinitionVersions)
                .Where(x => x.TenantId == tenantId && x.Id == id);

            var entity = await query.FirstOrDefaultAsync(cancellationToken);

            return Map(entity);
        }
        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowDefinitions.Where(x => x.TenantId == tenantId).Include(x => x.WorkflowDefinitionVersions).AsQueryable();

            var entities = await query.ToListAsync(cancellationToken);

            return mapper.Map<IEnumerable<WorkflowDefinition>>(entities);
        }
        public async Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var entity = await dbContext
                .WorkflowDefinitions
                .FirstOrDefaultAsync(x => x.TenantId == definition.TenantId && x.Id == definition.Id, cancellationToken);

            entity = mapper.Map(definition, entity);

            dbContext.WorkflowDefinitions.Update(entity);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(entity);
        }
        public async Task<int> DeleteAsync(string tenantId, string id, CancellationToken cancellationToken = default)
        {
            var definitions = await dbContext.WorkflowDefinitions
                .Where(x => x.TenantId == tenantId && x.Id == id)
                .Include(x => x.WorkflowDefinitionVersions)
                .ToListAsync(cancellationToken);

            foreach (var definition in definitions)
            {
                dbContext.WorkflowDefinitionVersions.RemoveRange(definition.WorkflowDefinitionVersions);
            }

            dbContext.WorkflowDefinitions.RemoveRange(definitions);

            await dbContext.SaveChangesAsync(cancellationToken);

            return definitions.Count;
        }

        private WorkflowDefinitionEntity Map(WorkflowDefinition source) => mapper.Map<WorkflowDefinitionEntity>(source);
        private WorkflowDefinition Map(WorkflowDefinitionEntity source) => mapper.Map<WorkflowDefinition>(source);
    }
}
