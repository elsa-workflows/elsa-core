using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowInstanceStore
    {   
        Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken);
        Task<WorkflowInstance> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<IEnumerable<WorkflowInstance>> ListAllAsync(string parentId, CancellationToken cancellationToken);
        Task<IEnumerable<WorkflowInstance>> ListAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Tuple<WorkflowInstance, IActivity>>> ListByBlockingActivityAsync(string workflowType, CancellationToken cancellationToken);
        Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(string parentId, WorkflowStatus status, CancellationToken cancellationToken);
        Task<IEnumerable<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken);
    }
    
    public static class WorkflowInstanceStoreExtensions
    {
        public static async Task<IEnumerable<Tuple<WorkflowInstance, TActivity>>> ListByBlockingActivityAsync<TActivity>(this IWorkflowInstanceStore store, CancellationToken cancellationToken) where TActivity : IActivity
        {
            var items = await store.ListByBlockingActivityAsync(nameof(TActivity), cancellationToken);
            return items.Select(x => Tuple.Create(x.Item1, (TActivity) x.Item2));
        }
    }
}