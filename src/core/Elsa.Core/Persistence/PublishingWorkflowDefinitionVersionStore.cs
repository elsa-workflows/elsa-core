using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messaging.Domain;
using Elsa.Models;
using MediatR;

namespace Elsa.Persistence
{
    public class PublishingWorkflowDefinitionVersionStore : IWorkflowDefinitionVersionStore
    {
        private readonly IWorkflowDefinitionVersionStore decoratedStore;
        private readonly IMediator mediator;

        public PublishingWorkflowDefinitionVersionStore(IWorkflowDefinitionVersionStore decoratedStore, IMediator mediator)
        {
            this.decoratedStore = decoratedStore;
            this.mediator = mediator;
        }
        
        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var savedDefinitionVersion = await decoratedStore.SaveAsync(definitionVersion, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return savedDefinitionVersion;
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var result = await decoratedStore.AddAsync(definitionVersion, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return result;
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return decoratedStore.GetByIdAsync(id, cancellationToken);
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string definitionId, VersionOptions version, CancellationToken cancellationToken = default)
        {
            return decoratedStore.GetByIdAsync(definitionId, version, cancellationToken);
        }

        public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            return decoratedStore.ListAsync(version, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var updatedDefinitionVersion = await decoratedStore.UpdateAsync(definitionVersion, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return updatedDefinitionVersion;
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var count = await decoratedStore.DeleteAsync(id, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return count;
        }

        private Task PublishUpdateEventAsync(CancellationToken cancellationToken)
        {
            return mediator.Publish(new WorkflowDefinitionVersionStoreUpdated(), cancellationToken);
        }
    }
}