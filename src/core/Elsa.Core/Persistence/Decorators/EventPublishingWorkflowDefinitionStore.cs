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
    public class EventPublishingWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IWorkflowDefinitionStore _store;
        private readonly IMediator _mediator;

        public EventPublishingWorkflowDefinitionStore(IWorkflowDefinitionStore store, IMediator mediator)
        {
            _store = store;
            _mediator = mediator;
        }

        public Task<int> CountAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken = default) => _store.CountAsync(specification, cancellationToken);

        public async Task DeleteAsync(WorkflowDefinition entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowDefinitionDeleting(entity), cancellationToken);
            await _store.DeleteAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowDefinitionDeleted(entity), cancellationToken);
        }

        public async Task<int> DeleteManyAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken = default)
        {
            var workflowDefinitions = await FindManyAsync(specification, cancellationToken: cancellationToken).ToList();

            if (!workflowDefinitions.Any())
                return 0;
            
            foreach (var workflowDefinition in workflowDefinitions)
                await _mediator.Publish(new WorkflowDefinitionDeleting(workflowDefinition), cancellationToken);

            await _mediator.Publish(new ManyWorkflowDefinitionsDeleting(workflowDefinitions), cancellationToken);

            var count = await _store.DeleteManyAsync(specification, cancellationToken);

            foreach (var instance in workflowDefinitions)
                await _mediator.Publish(new WorkflowDefinitionDeleted(instance), cancellationToken);

            await _mediator.Publish(new ManyWorkflowDefinitionsDeleted(workflowDefinitions), cancellationToken);

            return count;
        }

        public Task<WorkflowDefinition?> FindAsync(ISpecification<WorkflowDefinition> specification, CancellationToken cancellationToken = default) => _store.FindAsync(specification, cancellationToken);

        public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(ISpecification<WorkflowDefinition> specification, IOrderBy<WorkflowDefinition>? orderBy = null, IPaging? paging = null, CancellationToken cancellationToken = default)
            => _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public async Task SaveAsync(WorkflowDefinition entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowDefinitionSaving(entity), cancellationToken);
            await _store.SaveAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowDefinitionSaved(entity), cancellationToken);
        }

        public async Task AddAsync(WorkflowDefinition entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowDefinitionSaving(entity), cancellationToken);
            await _store.AddAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowDefinitionSaved(entity), cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<WorkflowDefinition> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();
            
            foreach (var entity in list)
                await _mediator.Publish(new WorkflowDefinitionSaving(entity), cancellationToken);
            
            await _store.AddManyAsync(list, cancellationToken);

            foreach (var entity in list)
                await _mediator.Publish(new WorkflowDefinitionSaved(entity), cancellationToken);
        }

        public async Task UpdateAsync(WorkflowDefinition entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowDefinitionSaving(entity), cancellationToken);
            await _store.UpdateAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowDefinitionSaved(entity), cancellationToken);
        }
    }
}