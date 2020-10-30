using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Data;
using Elsa.Events;
using Elsa.Models;
using MediatR;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Services
{
    public class WorkflowInstanceManager : IWorkflowInstanceManager
    {
        private readonly ISession _session;
        private readonly IMediator _mediator;

        public WorkflowInstanceManager(ISession session, IMediator mediator)
        {
            _session = session;
            _mediator = mediator;
        }

        public async ValueTask SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            _session.Save(workflowInstance, CollectionNames.WorkflowInstances);
            await _session.CommitAsync();
            await _mediator.Publish(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
        }

        public async ValueTask DeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            _session.Delete(workflowInstance, CollectionNames.WorkflowInstances);
            await _session.CommitAsync();
            await _mediator.Publish(new WorkflowInstanceDeleted(workflowInstance), cancellationToken);
        }

        public IQuery<WorkflowInstance> Query() => _session.Query<WorkflowInstance>(CollectionNames.WorkflowInstances);
        public IQuery<WorkflowInstance, TIndex> Query<TIndex>() where TIndex : class, IIndex => _session.Query<WorkflowInstance, TIndex>(CollectionNames.WorkflowInstances);
        public IQuery<WorkflowInstance, TIndex> Query<TIndex>(Expression<Func<TIndex, bool>> predicate) where TIndex : class, IIndex => _session.Query<WorkflowInstance, TIndex>(predicate, CollectionNames.WorkflowInstances);
        public IQuery<WorkflowInstance> ExecuteQuery(ICompiledQuery<WorkflowInstance> query) => _session.ExecuteQuery(query, CollectionNames.WorkflowInstances);
    }
}