using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowDefinitionStore
    {
        Task<WorkflowDefinition> SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
        Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
        Task<WorkflowDefinition> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowDefinition>> ListAsync(VersionOptions version, CancellationToken cancellationToken = default);
        Task<WorkflowDefinition> UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}