using Elsa.Retention.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Retention.CleanupStrategies;

/// <summary>
///     Deletes the workflow instance.
/// </summary>
public class DeleteWorkflowInstanceStrategy(IWorkflowInstanceStore store) : IDeletionCleanupStrategy<WorkflowInstance>
{
    public async Task Cleanup(ICollection<WorkflowInstance> collection)
    {
        await store.DeleteAsync(new WorkflowInstanceFilter
        {
            Ids = collection.Select(x => x.Id).ToArray()
        });
    }
}