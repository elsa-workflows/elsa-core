using System;
using System.Collections.Generic;
using System.Threading;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public interface ITriggerProvider
    {
        Type ForType();
        IAsyncEnumerable<ITrigger> GetTriggersAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default);
    }
}