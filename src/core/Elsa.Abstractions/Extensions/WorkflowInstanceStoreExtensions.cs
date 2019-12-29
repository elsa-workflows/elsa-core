using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using ProcessInstance = Elsa.Models.ProcessInstance;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceStoreExtensions
    {
        public static async Task<IEnumerable<(ProcessInstance, ActivityInstance)>> ListByBlockingActivityAsync<TActivity>(
            this IWorkflowInstanceStore store,
            string? correlationId = default,
            CancellationToken cancellationToken = default) where TActivity : IActivity
        {
            var items = await store.ListByBlockingActivityAsync(typeof(TActivity).Name, correlationId, cancellationToken);
            return items.Select(x => (x.Item1, x.Item2));
        }
    }
}