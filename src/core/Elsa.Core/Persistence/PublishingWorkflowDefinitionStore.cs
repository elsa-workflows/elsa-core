using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messages;
using Elsa.Models;
using MediatR;

namespace Elsa.Persistence
{
    public class PublishingWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IWorkflowDefinitionStore decoratedStore;
        private readonly IMediator mediator;

        public PublishingWorkflowDefinitionStore(IWorkflowDefinitionStore decoratedStore, IMediator mediator)
        {
            this.decoratedStore = decoratedStore;
            this.mediator = mediator;
        }
        
        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var savedDefinition = await decoratedStore.SaveAsync(definition, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return savedDefinition;
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var result = await decoratedStore.AddAsync(definition, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return result;
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            return decoratedStore.GetByIdAsync(id, version, cancellationToken);
        }

        public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            return decoratedStore.ListAsync(version, cancellationToken);
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var updatedDefinition = await decoratedStore.UpdateAsync(definition, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return updatedDefinition;
        }

        public async Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var count = await decoratedStore.DeleteAsync(id, cancellationToken);
            await PublishUpdateEventAsync(cancellationToken);
            return count;
        }

        private Task PublishUpdateEventAsync(CancellationToken cancellationToken)
        {
            return mediator.Publish(new WorkflowDefinitionStoreUpdated(), cancellationToken);
        }
    }
}