using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.Memory
{
    public class MemoryWorkflowDefinitionVersionStore : IWorkflowDefinitionVersionStore
    {
        private readonly List<WorkflowDefinitionVersion> definitionVersions;

        public MemoryWorkflowDefinitionVersionStore()
        {
            definitionVersions = new List<WorkflowDefinitionVersion>();
        }

        public async Task<WorkflowDefinitionVersion> SaveAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var existingDefinitionVersion = await GetByIdAsync(definitionVersion.Id, VersionOptions.SpecificVersion(definitionVersion.Version), cancellationToken);

            if (existingDefinitionVersion == null)
                await AddAsync(definitionVersion, cancellationToken);
            else
                await UpdateAsync(definitionVersion, cancellationToken);

            return definitionVersion;
        }

        public async Task<WorkflowDefinitionVersion> AddAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken = default)
        {
            var existingDefinitionVersion = await GetByIdAsync(definitionVersion.Id, VersionOptions.SpecificVersion(definitionVersion.Version), cancellationToken);

            if (existingDefinitionVersion != null)
            {
                throw new ArgumentException($"A workflow definition with ID '{definitionVersion.Id}' and version '{definitionVersion.Version}' already exists.");
            }

            definitionVersions.Add(definitionVersion);
            return definitionVersion;
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var definitionVersion = definitionVersions.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(definitionVersion);
        }

        public Task<WorkflowDefinitionVersion> GetByIdAsync(string definitionId, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = definitionVersions.Where(x => x.DefinitionId == definitionId).AsQueryable().WithVersion(version);
            var definitionVersion = query.FirstOrDefault();
            return Task.FromResult(definitionVersion);
        }

        public Task<IEnumerable<WorkflowDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = definitionVersions.AsQueryable().WithVersion(version);
            return Task.FromResult(query.AsEnumerable());
        }

        public async Task<WorkflowDefinitionVersion> UpdateAsync(WorkflowDefinitionVersion definitionVersion, CancellationToken cancellationToken)
        {
            var existingDefinitionVersion = await GetByIdAsync(definitionVersion.Id, VersionOptions.SpecificVersion(definitionVersion.Version), cancellationToken);
            var index = definitionVersions.IndexOf(existingDefinitionVersion);

            definitionVersions[index] = definitionVersion;
            return definitionVersion;
        }

        public Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var count = definitionVersions.RemoveAll(x => x.DefinitionId == id);
            return Task.FromResult(count);
        }
    }
}