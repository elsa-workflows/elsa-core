using System.Runtime.CompilerServices;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Runtime;

public static class WorkflowInstanceStoreExtensions
{
    public static async IAsyncEnumerable<WorkflowInstanceSummary> EnumerateSummariesAsync(
        this IWorkflowInstanceStore store, 
        WorkflowInstanceFilter filter, 
        int batchSize = 100, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageArgs = PageArgs.FromPage(0, batchSize);

        while (!cancellationToken.IsCancellationRequested)
        {
            var page = await store.SummarizeManyAsync(filter, pageArgs, cancellationToken);
            var workflowInstances = page.Items;

            if (workflowInstances.Count == 0)
                yield break;

            foreach (var workflowInstance in workflowInstances)
                yield return workflowInstance;

            pageArgs = pageArgs.Next();
        }
    }
}