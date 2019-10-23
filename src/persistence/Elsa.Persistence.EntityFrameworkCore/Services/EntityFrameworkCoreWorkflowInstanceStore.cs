using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Documents;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Services
{
    public class EntityFrameworkCoreWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly ElsaContext dbContext;
        private readonly IMapper mapper;

        public EntityFrameworkCoreWorkflowInstanceStore(
            ElsaContext dbContext,
            IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public async Task SaveAsync(
            WorkflowInstance instance,
            CancellationToken cancellationToken = default)
        {
            var document = Map(instance);

            await dbContext.WorkflowInstances.Upsert(document)
                .On(x => new { x.Id })
                .RunAsync(cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<WorkflowInstance> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var document = await dbContext.WorkflowInstances.FindAsync(new object[] { id }, cancellationToken);

            return Map(document);
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            var document = await dbContext.WorkflowInstances
                .Where(x => x.CorrelationId == correlationId)
                .FirstOrDefaultAsync(cancellationToken);

            return Map(document);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            string definitionId,
            CancellationToken cancellationToken = default)
        {
            var documents = await dbContext.WorkflowInstances
                .Where(x => x.DefinitionId == definitionId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            var documents = await dbContext.WorkflowInstances
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            return Map(documents);
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext.WorkflowInstances.AsQueryable();

            query = query.Where(x => x.Status == WorkflowStatus.Executing);

            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(x => x.CorrelationId == correlationId);

            query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType));
            query = query.OrderByDescending(x => x.CreatedAt);

            var documents = await query.ToListAsync(cancellationToken);
            var instances = Map(documents);

            return instances.GetBlockingActivities(activityType);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            string definitionId,
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var documents = await dbContext.WorkflowInstances
                .Where(x => x.DefinitionId == definitionId && x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var documents = await dbContext.WorkflowInstances
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Map(documents);
        }

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var record = await dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (record == null)
                return;

            dbContext.WorkflowInstances.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private WorkflowInstanceDocument Map(WorkflowInstance source) => mapper.Map<WorkflowInstanceDocument>(source);
        private WorkflowInstance Map(WorkflowInstanceDocument source) => mapper.Map<WorkflowInstance>(source);

        private IEnumerable<WorkflowInstance> Map(IEnumerable<WorkflowInstanceDocument> source) =>
            mapper.Map<IEnumerable<WorkflowInstance>>(source);
    }
}