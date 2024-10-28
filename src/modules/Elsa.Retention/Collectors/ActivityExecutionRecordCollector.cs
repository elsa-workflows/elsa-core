using Elsa.Retention.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Retention.Collectors;

/// <summary>
///     Collects all <see cref="ActivityExecutionRecord" /> related to the <see cref="WorkflowInstance" />
/// </summary>
public class ActivityExecutionRecordCollector : IRelatedEntityCollector<ActivityExecutionRecord>
{
    private readonly IActivityExecutionStore _store;

    public ActivityExecutionRecordCollector(IActivityExecutionStore store)
    {
        _store = store;
    }

    public async IAsyncEnumerable<ICollection<ActivityExecutionRecord>> GetRelatedEntities(ICollection<WorkflowInstance> workflowInstances)
    {
        IEnumerable<WorkflowInstance[]> chunks = workflowInstances.Chunk(5);

        foreach (WorkflowInstance[] chunk in chunks)
        {
            ActivityExecutionRecordFilter filter = new()
            {
                WorkflowInstanceIds = chunk.Select(x => x.Id).ToArray()
            };

            IEnumerable<ActivityExecutionRecord> records = await _store.FindManyAsync(filter);
            yield return records.ToArray();
        }
    }
}