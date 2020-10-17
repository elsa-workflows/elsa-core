using System;
using System.Collections.Generic;
using System.Threading;

namespace Elsa.Triggers
{
    public static class WorkflowSelectorExtensions
    {
        public static IAsyncEnumerable<WorkflowSelectorResult> SelectWorkflowsAsync<T>(
            this IWorkflowSelector workflowSelector,
            Func<T, bool> evaluate,
            CancellationToken cancellationToken = default) where T : ITrigger =>
            workflowSelector.SelectWorkflowsAsync(typeof(T), t => evaluate((T)t), cancellationToken);
    }
}