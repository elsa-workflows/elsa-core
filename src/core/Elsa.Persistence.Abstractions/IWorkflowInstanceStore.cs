using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowInstanceStore : IWorkflowStore
    {   
        Task<IEnumerable<Tuple<Workflow, IActivity>>> ListByBlockingActivityAsync(string workflowType, CancellationToken cancellationToken);
        Task<IEnumerable<Workflow>> ListByStatusAsync(string parentId, WorkflowStatus status, CancellationToken cancellationToken);
        Task<IEnumerable<Workflow>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken);
    }
    
    public static class WorkflowInstanceStoreExtensions
    {
        public static async Task<IEnumerable<Tuple<Workflow, TActivity>>> ListByBlockingActivityAsync<TActivity>(this IWorkflowInstanceStore store, CancellationToken cancellationToken) where TActivity : IActivity
        {
            var items = await store.ListByBlockingActivityAsync(nameof(TActivity), cancellationToken);
            return items.Select(x => Tuple.Create(x.Item1, (TActivity) x.Item2));
        }
    }
}