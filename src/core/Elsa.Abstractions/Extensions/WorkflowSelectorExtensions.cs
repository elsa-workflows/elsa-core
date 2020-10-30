using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Elsa.Triggers;

namespace Elsa
{
    public static class WorkflowSelectorExtensions
    {
        public static Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync<T>(
            this IWorkflowSelector workflowSelector,
            Func<T, bool> evaluate,
            CancellationToken cancellationToken = default) where T : ITrigger =>
            workflowSelector.SelectWorkflowsAsync(typeof(T), t => evaluate((T)t), cancellationToken);

        public static Task<IEnumerable<IActivityBlueprint>> GetTriggersAsync<T>(
            this IWorkflowSelector workflowSelector,
            Func<T, bool> evaluate,
            CancellationToken cancellationToken = default) where T : ITrigger =>
            workflowSelector.GetTriggersAsync(typeof(T), t => evaluate((T)t), cancellationToken);
    }
}