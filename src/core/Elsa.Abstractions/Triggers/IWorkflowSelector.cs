using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Triggers
{
    public interface IWorkflowSelector
    {
        Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(string activityType, IEnumerable<ITrigger> triggers, string? tenantId, CancellationToken cancellationToken = default);
    }
}