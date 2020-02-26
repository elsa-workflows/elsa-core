using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowInstanceStore
    {
        Task<WorkflowInstance> SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<(WorkflowInstance WorkflowInstance, BlockingActivity BlockingActivity)>> ListByBlockingActivityTagAsync(string activityType, string tag, string? correlationId = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<(WorkflowInstance WorkflowInstance, BlockingActivity BlockingActivity)>> ListByBlockingActivityAsync(string activityType, string? correlationId = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string definitionId, WorkflowStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}