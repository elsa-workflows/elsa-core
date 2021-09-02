using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Events;
using Elsa.WorkflowSettings.Models;
using MediatR;
using Open.Linq.AsyncExtensions;

namespace Elsa.WorkflowSettings.Persistence.Decorators
{
    public class EventPublishingWorkflowSettingsStore : IWorkflowSettingsStore
    {
        private readonly IWorkflowSettingsStore _store;
        private readonly IMediator _mediator;

        public EventPublishingWorkflowSettingsStore(IWorkflowSettingsStore store, IMediator mediator)
        {
            _store = store;
            _mediator = mediator;
        }

        public Task<int> CountAsync(ISpecification<WorkflowSetting> specification, CancellationToken cancellationToken = default) => _store.CountAsync(specification, cancellationToken);

        public async Task DeleteAsync(WorkflowSetting entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowSettingsDeleting(entity), cancellationToken);
            await _store.DeleteAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowSettingsDeleted(entity), cancellationToken);
        }

        public async Task<int> DeleteManyAsync(ISpecification<WorkflowSetting> specification, CancellationToken cancellationToken = default)
        {
            var webhookDefinitions = await FindManyAsync(specification, cancellationToken: cancellationToken).ToList();

            if (!webhookDefinitions.Any())
                return 0;

            foreach (var webhookDefinition in webhookDefinitions)
                await _mediator.Publish(new WorkflowSettingsDeleting(webhookDefinition), cancellationToken);

            await _mediator.Publish(new ManyWorkflowSettingsDeleting(webhookDefinitions), cancellationToken);

            var count = await _store.DeleteManyAsync(specification, cancellationToken);

            foreach (var instance in webhookDefinitions)
                await _mediator.Publish(new WorkflowSettingsDeleted(instance), cancellationToken);

            await _mediator.Publish(new ManyWorkflowSettingsDeleted(webhookDefinitions), cancellationToken);

            return count;
        }

        public Task<WorkflowSetting?> FindAsync(ISpecification<WorkflowSetting> specification, CancellationToken cancellationToken = default) => _store.FindAsync(specification, cancellationToken);

        public Task<IEnumerable<WorkflowSetting>> FindManyAsync(ISpecification<WorkflowSetting> specification, IOrderBy<WorkflowSetting>? orderBy = null, IPaging? paging = null, CancellationToken cancellationToken = default)
            => _store.FindManyAsync(specification, orderBy, paging, cancellationToken);

        public async Task SaveAsync(WorkflowSetting entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowSettingsSaving(entity), cancellationToken);
            await _store.SaveAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowSettingsSaved(entity), cancellationToken);
        }

        public async Task AddAsync(WorkflowSetting entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowSettingsSaving(entity), cancellationToken);
            await _store.AddAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowSettingsSaved(entity), cancellationToken);
        }

        public async Task AddManyAsync(IEnumerable<WorkflowSetting> entities, CancellationToken cancellationToken = default)
        {
            var list = entities.ToList();

            foreach (var entity in list)
                await _mediator.Publish(new WorkflowSettingsSaving(entity), cancellationToken);

            await _store.AddManyAsync(list, cancellationToken);

            foreach (var entity in list)
                await _mediator.Publish(new WorkflowSettingsSaved(entity), cancellationToken);
        }

        public async Task UpdateAsync(WorkflowSetting entity, CancellationToken cancellationToken = default)
        {
            await _mediator.Publish(new WorkflowSettingsSaving(entity), cancellationToken);
            await _store.UpdateAsync(entity, cancellationToken);
            await _mediator.Publish(new WorkflowSettingsSaved(entity), cancellationToken);
        }
    }
}