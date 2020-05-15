using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Services
{
    public class EntityFrameworkCoreWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly ElsaContext dbContext;
        private readonly IMapper mapper;

        public EntityFrameworkCoreWorkflowInstanceStore(ElsaContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public async Task<WorkflowInstance> SaveAsync(
            WorkflowInstance instance,
            CancellationToken cancellationToken = default)
        {
            var existingEntity = await dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .FirstOrDefaultAsync(x => x.InstanceId == instance.Id, cancellationToken: cancellationToken);

            if (existingEntity == null)
            {
                var entity = Map(instance);

                await dbContext.WorkflowInstances.AddAsync(entity, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Map(entity);
            }
            else
            {
                dbContext.ActivityInstances.RemoveRange(existingEntity.Activities);
                dbContext.BlockingActivities.RemoveRange(existingEntity.BlockingActivities);
                existingEntity.Activities.Clear();
                existingEntity.BlockingActivities.Clear();

                var entity = mapper.Map(instance, existingEntity);

                dbContext.WorkflowInstances.Update(entity);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Map(entity);
            }
        }

        public async Task<WorkflowInstance> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var document = await dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .FirstOrDefaultAsync(x => x.InstanceId == id, cancellationToken);

            return Map(document);
        }

        public async Task<WorkflowInstance> GetByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            var document = await dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .Where(x => x.CorrelationId == correlationId)
                .FirstOrDefaultAsync(cancellationToken);

            return Map(document);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(
            string definitionId,
            CancellationToken cancellationToken = default)
        {
            var documents = await dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .Where(x => x.DefinitionId == definitionId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            var documents = await dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            return Map(documents);
        }

        public async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .AsQueryable();

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
            var documents = await dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .Where(x => x.DefinitionId == definitionId && x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(
            WorkflowStatus status,
            CancellationToken cancellationToken = default)
        {
            var documents = await dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Map(documents);
        }

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var record = await dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.InstanceId == id, cancellationToken);

            if (record == null)
                return;

            var activityInstanceRecords = await dbContext.ActivityInstances
                .Where(x => x.WorkflowInstance.InstanceId == id)
                .ToListAsync(cancellationToken);

            var blockingActivityRecords = await dbContext.BlockingActivities
                .Where(x => x.WorkflowInstance.InstanceId == id)
                .ToListAsync(cancellationToken);
            
            dbContext.ActivityInstances.RemoveRange(activityInstanceRecords);
            dbContext.BlockingActivities.RemoveRange(blockingActivityRecords);
            dbContext.WorkflowInstances.Remove(record);
            
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private WorkflowInstanceEntity Map(WorkflowInstance source) => mapper.Map<WorkflowInstanceEntity>(source);
        private WorkflowInstance Map(WorkflowInstanceEntity source) => mapper.Map<WorkflowInstance>(source);
        private IEnumerable<WorkflowInstance> Map(IEnumerable<WorkflowInstanceEntity> source) => mapper.Map<IEnumerable<WorkflowInstance>>(source);
    }
}