using Elsa.Retention.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Retention.Collectors;

/// <summary>
///     Collects all <see cref="ActivityExecutionRecord" /> related to the <see cref="WorkflowInstance" />
/// </summary>
public class ActivityExecutionRecordCollector(IActivityExecutionStore store) : IRelatedEntityCollector<ActivityExecutionRecord>
{
    public async IAsyncEnumerable<ICollection<ActivityExecutionRecord>> GetRelatedEntities(ICollection<WorkflowInstance> workflowInstances)
    {
        var chunks = workflowInstances.Chunk(5);

        foreach (var chunk in chunks)
        {
            var filter = new ActivityExecutionRecordFilter()
            {
                WorkflowInstanceIds = chunk.Select(x => x.Id).ToArray()
            };

            var records = await store.FindManyAsync(filter);
            yield return records.ToArray();
        }
    }
}