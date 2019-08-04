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
        
        public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            await dbContext.WorkflowDefinitions.AddAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowDefinitions.AsQueryable().WithVersion(version);
            
            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowDefinitions.AsQueryable().WithVersion(version);
            return await query.ToListAsync(cancellationToken);
        }

        public Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
        {
            dbContext.WorkflowDefinitions.Update(definition);
            return Task.FromResult(definition);
        }
    }
}