using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messaging.Domain;
using Elsa.Models;
using MediatR;

namespace Elsa.Persistence
{
    public class PublishingWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IWorkflowDefinitionStore _decoratedStore;
        private readonly IMediator _mediator;

        public PublishingWorkflowDefinitionStore(IWorkflowDefinitionStore decoratedStore, IMediator mediator)
        {
            this._decoratedStore = decoratedStore;
            this._mediator = mediator;
        }
        
        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var savedDefinition = await _decoratedStore.SaveAsync(definition, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return savedDefinition;
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var result = await _decoratedStore.AddAsync(definition, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return result;
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return _decoratedStore.GetByIdAsync(id, cancellationToken);
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string definitionId, VersionOptions version, CancellationToken cancellationToken = default)
        {
            return _decoratedStore.GetByIdAsync(definitionId, version, cancellationToken);
        }

        public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            return _decoratedStore.ListAsync(version, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var updatedDefinition = await _decoratedStore.UpdateAsync(definition, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return updatedDefinition;
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var count = await _decoratedStore.DeleteAsync(id, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return count;
        }

        private Task PublishUpdateEventAsync(CancellationToken cancellationToken)
        {
            return _mediator.Publish(new WorkflowDefinitionStoreUpdated(), cancellationToken);
        }
    }
}