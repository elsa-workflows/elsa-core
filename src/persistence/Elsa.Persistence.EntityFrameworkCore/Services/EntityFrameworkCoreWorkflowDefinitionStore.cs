using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var query = dbContext.WorkflowDefinitions.AsQueryable();
            
            if (version.IsDraft)
                query = query.Where(x => !x.IsPublished).OrderByDescending(x => x.Version);
            else if(version.IsLatest)
                query = query.OrderByDescending(x => x.Version);
            else if(version.IsPublished)
                query = query.Where(x => x.IsPublished).OrderByDescending(x => x.Version);
            else if(version.Version > 0)
                query = query.Where(x => x.Version == version.Version);
            
            return await query.FirstOrDefaultAsync(cancellationToken);
        }
    }
}