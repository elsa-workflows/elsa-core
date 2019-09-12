using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Services
{
    public class EntityFrameworkCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly ElsaContext dbContext;

        public EntityFrameworkCoreWorkflowDefinitionStore(ElsaContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            await dbContext.WorkflowDefinitionVersions.Upsert(definition)
                .On(x => new { x.Id, x.Version })
                .RunAsync(cancellationToken);

            return definition;
        }

        public async Task AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            await dbContext.WorkflowDefinitionVersions.AddAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowDefinitionVersions
                .AsQueryable()
                .Where(x => x.DefinitionId == id)
                .WithVersion(version);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowDefinitionVersions.AsQueryable().WithVersion(version);
            return await query.ToListAsync(cancellationToken);
        }

        public Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken)
        {
            dbContext.WorkflowDefinitionVersions.Update(definition);
            return Task.FromResult(definition);
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var records = await dbContext.WorkflowDefinitionVersions.Where(x => x.DefinitionId == id).ToListAsync(cancellationToken);
            
            dbContext.WorkflowDefinitionVersions.RemoveRange(records);
            return records.Count;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}