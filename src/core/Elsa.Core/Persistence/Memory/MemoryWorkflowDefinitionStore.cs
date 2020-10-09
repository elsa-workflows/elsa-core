using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.Memory
{
    public class MemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly List<WorkflowDefinitionVersion> _definitions;

        public MemoryWorkflowDefinitionStore()
        {
            _definitions = new List<WorkflowDefinitionVersion>();
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var existingDefinition = await GetByIdAsync(definition.Id, VersionOptions.SpecificVersion(definition.Version), cancellationToken);

            if (existingDefinition == null)
                await AddAsync(definition, cancellationToken);
            else
                await UpdateAsync(definition, cancellationToken);

            return definition;
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken = default)
        {
            var existingDefinition = await GetByIdAsync(definition.Id, VersionOptions.SpecificVersion(definition.Version), cancellationToken);

            if (existingDefinition != null)
            {
                throw new ArgumentException($"A workflow definition with ID '{definition.Id}' and version '{definition.Version}' already exists.");
            }

            _definitions.Add(definition);
            return definition;
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var definition = _definitions.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(definition);
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string definitionId, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = _definitions.Where(x => x.DefinitionId == definitionId).AsQueryable().WithVersion(version);
            var definition = query.FirstOrDefault();
            return Task.FromResult(definition);
        }

        public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = _definitions.AsQueryable().WithVersion(version);
            return Task.FromResult(query.AsEnumerable());
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definition, CancellationToken cancellationToken)
        {
            var existingDefinition = await GetByIdAsync(definition.Id, VersionOptions.SpecificVersion(definition.Version), cancellationToken);
            var index = _definitions.IndexOf(existingDefinition);

            _definitions[index] = definition;
            return definition;
        }

        public Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var count = _definitions.RemoveAll(x => x.DefinitionId == id);
            return Task.FromResult(count);
        }
    }
}