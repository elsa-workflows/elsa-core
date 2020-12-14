using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Services
{
    public class WorkflowDefinitionManager : IWorkflowDefinitionManager
    {
        private readonly IIdGenerator _idGenerator;

        public WorkflowDefinitionManager(IWorkflowDefinitionStore store, IIdGenerator idGenerator)
        {
            _idGenerator = idGenerator;
            Store = store;
        }

        public IWorkflowDefinitionStore Store { get; }

        public async Task SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            workflowDefinition = Initialize(workflowDefinition);
            await Store.SaveAsync(workflowDefinition, cancellationToken);
        }

        public Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default) => Store.DeleteAsync(workflowDefinition, cancellationToken);
        public Task<int> CountAsync(VersionOptions? version = default, CancellationToken cancellationToken = default) => Store.CountAsync(version, cancellationToken);

        public Task<IEnumerable<WorkflowDefinition>> ListAsync(int? skip = default, int? take = default, VersionOptions? version = default, CancellationToken cancellationToken = default) =>
            Store.ListAsync(skip, take, version, cancellationToken);

        public async Task<WorkflowDefinition?> GetAsync(string workflowDefinitionId, VersionOptions version, CancellationToken cancellationToken = default) => await Store.GetAsync(workflowDefinitionId, version, cancellationToken);
        public async Task<WorkflowDefinition?> GetByVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default) => await Store.GetByVersionIdAsync(workflowDefinitionVersionId, cancellationToken);

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
    }
}