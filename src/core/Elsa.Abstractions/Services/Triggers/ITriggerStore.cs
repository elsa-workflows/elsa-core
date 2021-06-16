using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Triggers
{
    public interface ITriggerStore
    {
        ValueTask StoreAsync(IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default);
        ValueTask<IEnumerable<WorkflowTrigger>> GetAsync(CancellationToken cancellationToken = default);
    }
}