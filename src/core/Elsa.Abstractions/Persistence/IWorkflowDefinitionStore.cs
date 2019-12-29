using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowDefinitionStore
    {
        Task<ProcessDefinitionVersion> SaveAsync(ProcessDefinitionVersion definition, CancellationToken cancellationToken = default);
        Task<ProcessDefinitionVersion> AddAsync(ProcessDefinitionVersion definition, CancellationToken cancellationToken = default);
        Task<ProcessDefinitionVersion> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<ProcessDefinitionVersion> GetByIdAsync(string definitionId, VersionOptions version, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProcessDefinitionVersion>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default);
        Task<ProcessDefinitionVersion> UpdateAsync(ProcessDefinitionVersion definition, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}