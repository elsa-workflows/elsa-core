using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowDefinitionStore : InMemoryStore<WorkflowDefinition>, IWorkflowDefinitionStore
    {
        public InMemoryWorkflowDefinitionStore(IIdGenerator idGenerator) : base(idGenerator)
        {
        }

        public Task<int> CountAsync(VersionOptions? version = null, CancellationToken cancellationToken = default) => Task.FromResult(Entities.Values.WithVersion(version).Count());

        public Task<WorkflowDefinition> GetAsync(string workflowDefinitionId, VersionOptions version, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entities.Values.WithVersion(version).FirstOrDefault(x => x.EntityId == workflowDefinitionId));

        public Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entities.Values.FirstOrDefault(x => x.DefinitionVersionId == workflowDefinitionVersionId));

        public Task<IEnumerable<WorkflowDefinition>> ListAsync(int? skip = null, int? take = null, VersionOptions? version = null, CancellationToken cancellationToken = default)
        {
            var query = Entities.Values.WithVersion();

            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);

            return Task.FromResult(query);
        }
    }
}