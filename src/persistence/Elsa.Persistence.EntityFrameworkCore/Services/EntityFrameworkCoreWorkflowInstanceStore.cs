using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Services
{
    public class EntityFrameworkCoreWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly ElsaContext dbContext;

        public EntityFrameworkCoreWorkflowInstanceStore(ElsaContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
        {
            await dbContext.WorkflowInstances.AddAsync(instance, cancellationToken);
        }

        public async Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await dbContext.WorkflowInstances.FindAsync(id, cancellationToken);
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            return await dbContext.WorkflowInstances.Where(x => x.CorrelationId == correlationId).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            return await dbContext.WorkflowInstances.Where(x => x.DefinitionId == definitionId).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            return await dbContext.WorkflowInstances.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowInstances.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));

            if (string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(x => x.CorrelationId == correlationId);

            var instances = await query.ToListAsync(cancellationToken);

            return instances.GetBlockingActivities();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string definitionId, WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            return await dbContext.WorkflowInstances.Where(x => x.DefinitionId == definitionId && x.Status == status).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            return await dbContext.WorkflowInstances.Where(x => x.Status == status).ToListAsync(cancellationToken);
        }
    }
}