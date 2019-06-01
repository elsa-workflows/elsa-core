using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowDefinitionStore : IWorkflowStore
    {
        Task<IEnumerable<Tuple<Workflow, IActivity>>> ListByStartActivityAsync(string activityType, CancellationToken cancellationToken);
    }

    public static class WorkflowDefinitionStoreExtensions
    {
        public static async Task<IEnumerable<Tuple<Workflow, TActivity>>> ListByStartActivityAsync<TActivity>(this IWorkflowDefinitionStore store, CancellationToken cancellationToken) where TActivity : IActivity
        {
            var items = await store.ListByStartActivityAsync(typeof(TActivity).Name, cancellationToken);
            return items.Select(x => Tuple.Create(x.Item1, (TActivity) x.Item2));
        }
    }
}