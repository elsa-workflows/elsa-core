using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using WorkflowInstance = Elsa.Models.WorkflowInstance;

namespace Elsa.Persistence
{
    public interface IWorkflowInstanceStore
    {   
        Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync(string activityType, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string definitionId, WorkflowStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);
    }

    public static class WorkflowInstanceStoreExtensions
    {
        public static async Task<IEnumerable<(WorkflowInstance, ActivityInstance)>> ListByBlockingActivityAsync<TActivity>(this IWorkflowInstanceStore store, CancellationToken cancellationToken) where TActivity : IActivity
        {
            var items = await store.ListByBlockingActivityAsync(nameof(TActivity), cancellationToken);
            return items.Select(x => (x.Item1, x.Item2));
        }
    }
}