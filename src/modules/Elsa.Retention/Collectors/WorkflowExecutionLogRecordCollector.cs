using Elsa.Common.Models;
using Elsa.Retention.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Retention.Collectors;

/// <summary>
///     Collects all <see cref="WorkflowExecutionLogRecord" /> related to the <see cref="WorkflowInstance" />
/// </summary>
public class WorkflowExecutionLogRecordCollector : IRelatedEntityCollector<WorkflowExecutionLogRecord>
{
    private readonly IWorkflowExecutionLogStore _store;

    public WorkflowExecutionLogRecordCollector(IWorkflowExecutionLogStore store)
    {
        _store = store;
    }

    public async IAsyncEnumerable<ICollection<WorkflowExecutionLogRecord>> GetRelatedEntities(ICollection<WorkflowInstance> workflowInstances)
    {
        IEnumerable<WorkflowInstance[]> chunks = workflowInstances.Chunk(25);

        foreach (WorkflowInstance[] chunk in chunks)
        {
            WorkflowExecutionLogRecordFilter filter = new()
            {
                WorkflowInstanceIds = chunk.Select(x => x.Id).ToArray()
            };

            PageArgs pageArgs = PageArgs.FromPage(0, 100);

            while (true)
            {
                Page<WorkflowExecutionLogRecord> page = await _store.FindManyAsync(filter, pageArgs);
                yield return page.Items.ToArray();

                if (page.TotalCount <= pageArgs.Offset + page.Items.Count)
                {
                    break;
                }

                pageArgs.Next();
            }
        }
    }
}