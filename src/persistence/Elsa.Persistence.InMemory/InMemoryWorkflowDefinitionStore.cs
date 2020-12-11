using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private static readonly List<WorkflowDefinition> Definitions = new();
        private readonly IIdGenerator _idGenerator;

        public InMemoryWorkflowDefinitionStore(IIdGenerator idGenerator)
        {
            _idGenerator = idGenerator;
        }

        public Task<int> CountAsync(VersionOptions? version = null, CancellationToken cancellationToken = default) => Task.FromResult(Definitions.WithVersion(version).Count());

        public Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            Definitions.Remove(workflowDefinition);
            return Task.CompletedTask;
        }

        public Task<WorkflowDefinition> GetAsync(string workflowDefinitionId, VersionOptions version, CancellationToken cancellationToken = default) =>
            Task.FromResult(Definitions.WithVersion(version).FirstOrDefault(x => x.WorkflowDefinitionId == workflowDefinitionId));

        public Task<WorkflowDefinition> GetByVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Definitions.FirstOrDefault(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId));

        public WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition)
        {
            if (string.IsNullOrWhiteSpace(workflowDefinition.WorkflowDefinitionId))
                workflowDefinition.WorkflowDefinitionId = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (string.IsNullOrWhiteSpace(workflowDefinition.WorkflowDefinitionVersionId))
                workflowDefinition.WorkflowDefinitionVersionId = _idGenerator.Generate();

            return workflowDefinition;
        }

        public Task<IEnumerable<WorkflowDefinition>> ListAsync(int? skip = null, int? take = null, VersionOptions? version = null, CancellationToken cancellationToken = default)
        {
            var query = Definitions.WithVersion();

            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);

            return Task.FromResult(query);
        }

        public Task SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            workflowDefinition = Initialize(workflowDefinition);
            Definitions.Add(workflowDefinition);
            return Task.CompletedTask;
        }
    }
}