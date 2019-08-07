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
        private readonly List<WorkflowDefinition> definitions;

        public MemoryWorkflowDefinitionStore()
        {
            definitions = new List<WorkflowDefinition>();
        }

        public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            var existingDefinition = await GetByIdAsync(definition.Id, VersionOptions.SpecificVersion(definition.Version), cancellationToken);

            if (existingDefinition != null)
            {
                throw new ArgumentException($"A workflow definition with ID '{definition.Id}' and version '{definition.Version}' already exists.");
            }

            definitions.Add(definition);
        }

        public Task<WorkflowDefinition> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = definitions.Where(x => x.Id == id).AsQueryable().WithVersion(version);
            var definition = query.FirstOrDefault();
            return Task.FromResult(definition);
        }

        public Task<IEnumerable<WorkflowDefinition>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = definitions.AsQueryable().WithVersion(version);
            return Task.FromResult(query.AsEnumerable());
        }

        public async Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
        {
            var existingDefinition = await GetByIdAsync(definition.Id, VersionOptions.SpecificVersion(definition.Version), cancellationToken);
            var index = definitions.IndexOf(existingDefinition);

            definitions[index] = definition;
            return definition;
        }

        public Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var count = definitions.RemoveAll(x => x.Id == id);
            return Task.FromResult<int>(count);
        }
    }
}