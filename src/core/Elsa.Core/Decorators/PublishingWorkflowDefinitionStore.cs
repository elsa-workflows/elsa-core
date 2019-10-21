using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Decorators
{
    public class PublishingWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IWorkflowDefinitionStore decoratedStore;

        public PublishingWorkflowDefinitionStore(IWorkflowDefinitionStore decoratedStore)
        {
            this.decoratedStore = decoratedStore;
        }
        
        public Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            return decoratedStore.SaveAsync(definition, cancellationToken);
        }

        public Task AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            return decoratedStore.AddAsync(definition, cancellationToken);
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            return decoratedStore.GetByIdAsync(id, version, cancellationToken);
        }

        public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            return decoratedStore.ListAsync(version, cancellationToken);
        }

        public Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            return decoratedStore.UpdateAsync(definition, cancellationToken);
        }

        public Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            return decoratedStore.DeleteAsync(id, cancellationToken);
        }
    }
}