using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowInstanceStore
    {   
        Task<ProcessInstance> SaveAsync(ProcessInstance instance, CancellationToken cancellationToken = default);
        Task<ProcessInstance> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<ProcessInstance> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProcessInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProcessInstance>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<(ProcessInstance, ActivityInstance)>> ListByBlockingActivityAsync(string activityType, string? correlationId = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProcessInstance>> ListByStatusAsync(string definitionId, ProcessStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProcessInstance>> ListByStatusAsync(ProcessStatus status, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}