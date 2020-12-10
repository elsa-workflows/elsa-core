using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Data;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Repositories;

using MediatR;

using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql
{
    public class YesSqlWorkflowInstanceRepository : IWorkflowInstanceRepository
    {
        private readonly ISession _session;
        private readonly IMediator _mediator;

        public YesSqlWorkflowInstanceRepository(ISession session, IMediator mediator)
        {
            _session = session;
            _mediator = mediator;
        }

        public async Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            _session.Save(workflowInstance, CollectionNames.WorkflowInstances);
            await _session.CommitAsync();
            await _mediator.Publish(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
        }

        public async Task DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            _session.Delete(workflowInstance, CollectionNames.WorkflowInstances);
            await _session.CommitAsync();
            await _mediator.Publish(new WorkflowInstanceDeleted(workflowInstance), cancellationToken);
        }

        public async Task<bool> IsWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
           var instance = await  Query<WorkflowInstanceIndex>(index => index.WorkflowDefinitionId == workflowDefinitionId && index.TenantId == tenantId && (index.WorkflowStatus == WorkflowStatus.Running || index.WorkflowStatus == WorkflowStatus.Suspended))
                .FirstOrDefaultAsync();

            return instance != null;
        }

        public Task<WorkflowInstance> GetByIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            return Query<WorkflowInstanceIndex>(x => x.WorkflowInstanceId == workflowInstanceId).FirstOrDefaultAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) {
            return Query<WorkflowInstanceIndex>().Skip(page * pageSize).Take(pageSize).ListAsync();
        }
         
        public Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityTypeAsync(string activityType, CancellationToken cancellationToken = default)
        {
            return Query<WorkflowInstanceBlockingActivitiesIndex>(x => x.ActivityType == activityType).ListAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, string? tenantId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default)
        {
            return Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId && x.WorkflowStatus == workflowStatus).ListAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus workflowStatus, CancellationToken cancellationToken = default)
        {
            return Query<WorkflowInstanceIndex>(x => x.WorkflowStatus == workflowStatus).ListAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync( string workflowDefinitionId,string? tenantId,CancellationToken cancellationToken = default)
        {
            return Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId).ListAsync();
        }

        public Task<WorkflowInstance?> GetByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            return Query<WorkflowInstanceIndex>(x => x.CorrelationId == correlationId && x.WorkflowStatus == status).FirstOrDefaultAsync();
        }

        public Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default)
        {
            return Query<WorkflowInstanceIndex>(x => x.CorrelationId == correlationId && x.WorkflowStatus == status).ListAsync();
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return Query<WorkflowInstanceIndex>().CountAsync();
        }

        private IQuery<WorkflowInstance, TIndex> Query<TIndex>() where TIndex : class, IIndex => _session.Query<WorkflowInstance, TIndex>(CollectionNames.WorkflowInstances);
        private IQuery<WorkflowInstance, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => _session.Query<WorkflowInstance, TIndex>(predicate, CollectionNames.WorkflowInstances);
    }
}
