using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Triggers
{
    public interface IWorkflowTriggerProvider
    {
        string ForActivityType { get; }
        ValueTask<IEnumerable<ITrigger>> GetTriggersAsync(TriggerProviderContext context, CancellationToken cancellationToken = default);
    }
}