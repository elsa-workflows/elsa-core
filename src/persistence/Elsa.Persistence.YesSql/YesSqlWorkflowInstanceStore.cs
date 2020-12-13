using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Data;
using Elsa.Models;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Specifications;
using NetBox.Extensions;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql
{
    public class YesSqlWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly ISession _session;

        public YesSqlWorkflowInstanceStore(ISession session)
        {
            _session = session;
        }

        public async Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            _session.Save(workflowInstance, CollectionNames.WorkflowInstances);
            await _session.CommitAsync();
        }

        public async Task DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            _session.Delete(workflowInstance, CollectionNames.WorkflowInstances);
            await _session.CommitAsync();
        }

        public async Task<IEnumerable<WorkflowInstance>> ListAsync(ISpecification<WorkflowInstance> specification, IGroupingSpecification<WorkflowInstance>? grouping, IPagingSpecification? paging, CancellationToken cancellationToken)
        {
            return await Query<WorkflowInstanceIndex>().Apply(specification).Apply(grouping).Apply(paging).ListAsync();
        }

        public async Task<int> CountAsync(ISpecification<WorkflowInstance> specification, IGroupingSpecification<WorkflowInstance>? grouping = default, CancellationToken cancellationToken = default) => 
            await Query<WorkflowInstanceIndex>().Apply(specification).Apply(grouping).CountAsync();

        public async Task<WorkflowInstance?> FindAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default)
        {
            var query = Query<WorkflowInstanceIndex>().Where(specification.ToExpression().ConvertType<WorkflowInstance, WorkflowInstanceIndex>());
            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            var instance = await Query<WorkflowInstanceIndex>(index =>
                    index.WorkflowDefinitionId == workflowDefinitionId && index.TenantId == tenantId && (index.WorkflowStatus == WorkflowStatus.Running || index.WorkflowStatus == WorkflowStatus.Suspended))
                .FirstOrDefaultAsync();

            return instance != null;
        }

        public async Task<WorkflowInstance?> GetByIdAsync(string workflowInstanceId, CancellationToken cancellationToken = default) =>
            await Query<WorkflowInstanceIndex>(x => x.WorkflowInstanceId == workflowInstanceId).FirstOrDefaultAsync();

        public Task<IEnumerable<WorkflowInstance>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Query<WorkflowInstanceIndex>().Skip(page * pageSize).Take(pageSize).ListAsync();

        public Task<IEnumerable<WorkflowInstance>> ListByBlockingActivityTypeAsync(string activityType, CancellationToken cancellationToken = default) =>
            Query<WorkflowInstanceBlockingActivitiesIndex>(x => x.ActivityType == activityType).ListAsync();

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAndStatusAsync(string workflowDefinitionId, string? tenantId, WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) =>
            Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId && x.WorkflowStatus == workflowStatus).ListAsync();

        public Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus workflowStatus, CancellationToken cancellationToken = default) => Query<WorkflowInstanceIndex>(x => x.WorkflowStatus == workflowStatus).ListAsync();

        public Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string workflowDefinitionId, string? tenantId, CancellationToken cancellationToken = default) =>
            Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == workflowDefinitionId && x.TenantId == tenantId).ListAsync();

        public async Task<WorkflowInstance?> GetByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            await Query<WorkflowInstanceIndex>(x => x.CorrelationId == correlationId && x.WorkflowStatus == status).FirstOrDefaultAsync();

        public Task<IEnumerable<WorkflowInstance>> ListByCorrelationIdAsync(string correlationId, WorkflowStatus status, CancellationToken cancellationToken = default) =>
            Query<WorkflowInstanceIndex>(x => x.CorrelationId == correlationId && x.WorkflowStatus == status).ListAsync();

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Query<WorkflowInstanceIndex>().CountAsync();

        private IQuery<WorkflowInstance, TIndex> Query<TIndex>() where TIndex : class, IIndex => _session.Query<WorkflowInstance, TIndex>(CollectionNames.WorkflowInstances);
        private IQuery<WorkflowInstance, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => _session.Query<WorkflowInstance, TIndex>(predicate, CollectionNames.WorkflowInstances);
    }
}