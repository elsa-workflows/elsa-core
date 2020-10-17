using System;
using System.Collections.Generic;
using System.Threading;

namespace Elsa.Triggers
{
    public interface IWorkflowSelector
    {
        IAsyncEnumerable<WorkflowSelectorResult> SelectWorkflowsAsync(
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            CancellationToken cancellationToken = default);
    }
}