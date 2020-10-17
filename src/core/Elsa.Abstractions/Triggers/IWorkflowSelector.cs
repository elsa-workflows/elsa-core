using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Elsa.Services.Models;

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