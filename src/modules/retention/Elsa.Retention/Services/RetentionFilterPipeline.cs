using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Retention.Contracts;
using Elsa.Retention.Models;

namespace Elsa.Retention.Services;

public class RetentionFilterPipeline : IRetentionFilterPipeline
{
    private readonly IList<IRetentionFilter> _filters = new List<IRetentionFilter>();

    public void AddFilter(IRetentionFilter filter) => _filters.Add(filter);

    public async Task<IEnumerable<WorkflowInstance>> FilterAsync(IEnumerable<WorkflowInstance> workflowInstances, CancellationToken cancellationToken) =>
        await FilterInternalAsync(workflowInstances, cancellationToken).ToListAsync(cancellationToken);

    private async IAsyncEnumerable<WorkflowInstance> FilterInternalAsync(IEnumerable<WorkflowInstance> workflowInstances, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var workflowInstance in workflowInstances)
            if (await GetShouldDeleteAsync(workflowInstance, cancellationToken))
                yield return workflowInstance;
    }

    private async Task<bool> GetShouldDeleteAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var context = new RetentionFilterContext(workflowInstance, cancellationToken);

        foreach (var filter in _filters)
            if (await filter.GetShouldDeleteAsync(context))
                return true;

        return false;
    }
}