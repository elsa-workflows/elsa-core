using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.Core.DbContexts;
using Elsa.Persistence.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.Core.Services
{
    public class EntityFrameworkCoreWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly ElsaContext _dbContext;
        private readonly IMapper _mapper;

        public EntityFrameworkCoreWorkflowInstanceStore(ElsaContext dbContext, IMapper mapper)
        {
            this._dbContext = dbContext;
            this._mapper = mapper;
        }

        public async Task<WorkflowInstance> SaveAsync(
            WorkflowInstance instance,
            CancellationToken cancellationToken = default)
        {
            var existingEntity = await _dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .FirstOrDefaultAsync(x => x.InstanceId == instance.Id, cancellationToken: cancellationToken);

            if (existingEntity == null)
            {
                var entity = Map(instance);

                await _dbContext.WorkflowInstances.AddAsync(entity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Map(entity);
            }
            else
            {
                _dbContext.ActivityInstances.RemoveRange(existingEntity.Activities);
                _dbContext.BlockingActivities.RemoveRange(existingEntity.BlockingActivities);
                existingEntity.Activities.Clear();
                existingEntity.BlockingActivities.Clear();

                var entity = _mapper.Map(instance, existingEntity);

                _dbContext.WorkflowInstances.Update(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Map(entity);
            }
        }

        public async Task<WorkflowInstance> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var document = await _dbContext
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
            var document = await _dbContext
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
            var documents = await _dbContext
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
            var documents = await _dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
            return Map(documents);
        }
        public async Task<IEnumerable<(WorkflowInstance, BlockingActivity)>> ListByBlockingActivityTagAsync(
            string activityType,
            string tag,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .AsQueryable();

            query = query.Where(x => x.Status == WorkflowStatus.Suspended);

            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(x => x.CorrelationId == correlationId);

            query = query.Where(x => x.BlockingActivities.Any(y => y.ActivityType == activityType && y.Tag == tag));
            query = query.OrderByDescending(x => x.CreatedAt);

            var documents = await query.ToListAsync(cancellationToken);
            var instances = Map(documents);

            return instances.GetBlockingActivities(activityType);
        }

        public async Task<IEnumerable<(WorkflowInstance, BlockingActivity)>> ListByBlockingActivityAsync(
            string activityType,
            string correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .AsQueryable();

            query = query.Where(x => x.Status == WorkflowStatus.Suspended);

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
            var documents = await _dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .Where(x => x.DefinitionId == definitionId && x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Map(documents);
        }

        public async Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            var documents = await _dbContext
                .WorkflowInstances
                .Include(x => x.Activities)
                .Include(x => x.BlockingActivities)
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return Map(documents);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var record = await _dbContext.WorkflowInstances.FirstOrDefaultAsync(x => x.InstanceId == id, cancellationToken);

            if (record == null)
                return;

            var activityInstanceRecords = await _dbContext.ActivityInstances
                .Where(x => x.WorkflowInstance.InstanceId == id)
                .ToListAsync(cancellationToken);

            var blockingActivityRecords = await _dbContext.BlockingActivities
                .Where(x => x.WorkflowInstance.InstanceId == id)
                .ToListAsync(cancellationToken);

            _dbContext.ActivityInstances.RemoveRange(activityInstanceRecords);
            _dbContext.BlockingActivities.RemoveRange(blockingActivityRecords);
            _dbContext.WorkflowInstances.Remove(record);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private WorkflowInstanceEntity Map(WorkflowInstance source) => _mapper.Map<WorkflowInstanceEntity>(source);
        private WorkflowInstance Map(WorkflowInstanceEntity source) => _mapper.Map<WorkflowInstance>(source);
        private IEnumerable<WorkflowInstance> Map(IEnumerable<WorkflowInstanceEntity> source) => _mapper.Map<IEnumerable<WorkflowInstance>>(source);
    }
}