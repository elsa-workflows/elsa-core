using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Serialization.Models;
using Elsa.Services.Models;

namespace Elsa.Persistence
{
    public interface IWorkflowDefinitionStore
    {
        Task<IEnumerable<Tuple<WorkflowDefinition, ActivityDefinition>>> ListByStartActivityAsync(string activityType, CancellationToken cancellationToken);
    }

    public static class WorkflowDefinitionStoreExtensions
    {
        public static Task<IEnumerable<Tuple<WorkflowDefinition, ActivityDefinition>>> ListByStartActivityAsync<TActivity>(
            this IWorkflowDefinitionStore store, 
            CancellationToken cancellationToken) 
            where TActivity : IActivity
        {
            return store.ListByStartActivityAsync(typeof(TActivity).Name, cancellationToken);
        }
        
        public static async Task<IEnumerable<Tuple<WorkflowDefinition, ActivityDefinition>>> ListByStartActivityAsync(
            this IWorkflowDefinitionStore store, 
            string activityTypeName,
            CancellationToken cancellationToken) 
        {
            var items = await store.ListByStartActivityAsync(activityTypeName, cancellationToken);
            return items.Select(x => Tuple.Create(x.Item1, x.Item2));
        }
    }
}