using System;
using System.Collections.Generic;
using System.Threading;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public abstract class TriggerProvider<T> : ITriggerProvider where T : ITrigger
    {
        public Type ForType() => typeof(T);
        public abstract IAsyncEnumerable<ITrigger> GetTriggersAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default);
    }
}