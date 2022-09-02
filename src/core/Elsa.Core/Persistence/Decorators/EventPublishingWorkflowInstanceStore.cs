using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using MediatR;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.Decorators
{
    public class EventPublishingWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IWorkflowInstanceStore _store;
        private readonly IMediator _mediator;

        public EventPublishingWorkflowInstanceStore(IWorkflowInstanceStore store, IMediator mediator)
        {
            _store = store;
            _mediator = mediator;
        }

        public async Task SaveAsync(WorkflowInstance entity, CancellationToken cancellationToken = default)
        {
            await _store.SaveAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowInstanceSaved(entity), cancellationToken);
        }
        
        public async Task UpdateAsync(WorkflowInstance entity, CancellationToken cancellationToken = default)
        {
            await _store.UpdateAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowInstanceSaved(entity), cancellationToken);
        }

        public async Task AddAsync(WorkflowInstance entity, CancellationToken cancellationToken = default)
        {
            await _store.AddAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowInstanceSaved(entity), cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<WorkflowInstance> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();
            await _store.AddManyAsync(list, cancellationToken);

            foreach (var entity in list) 
                await _mediator.Publish(new WorkflowInstanceSaved(entity), cancellationToken);
            
            await _mediator.Publish(new ManyWorkflowInstancesAdded(list), cancellationToken);
        }

        public async Task DeleteAsync(WorkflowInstance entity, CancellationToken cancellationToken = default)
        {
            await _store.DeleteAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowInstanceDeleted(entity), cancellationToken);
        }

        public async Task<int> DeleteManyAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default)
        {
            var instancesId = await FindManyAsync<string>(specification, (wf)=> wf.Id,cancellationToken: cancellationToken).ToList();
            var count = await _store.DeleteManyAsync(specification, cancellationToken);

            if (instancesId.Any())
            {
                var instances = instancesId.Select(id => new WorkflowInstance() { Id = id });
                foreach (var instance in instances)
                    await _mediator.Publish(new WorkflowInstanceDeleted(instance), cancellationToken);

                await _mediator.Publish(new ManyWorkflowInstancesDeleted(instances), cancellationToken);
            }

            return count;
        }

        public Task<IEnumerable<WorkflowInstance>> FindManyAsync(ISpecification<WorkflowInstance> specification, IOrderBy<WorkflowInstance>? orderBy = default, IPaging? paging = default, CancellationToken cancellationToken = default) =>
            _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public Task<int> CountAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default) => _store.CountAsync(specification, cancellationToken);

        public Task<WorkflowInstance?> FindAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default) => _store.FindAsync(specification, cancellationToken);

        public Task<IEnumerable<TOut>> FindManyAsync<TOut>(ISpecification<WorkflowInstance> specification
            , System.Linq.Expressions.Expression<System.Func<WorkflowInstance, TOut>> funcMapping, IOrderBy<WorkflowInstance>? orderBy = null, IPaging? paging = null, CancellationToken cancellationToken = default) where TOut : class
        => _store.FindManyAsync<TOut>(specification, funcMapping, orderBy,paging, cancellationToken);
    }
}