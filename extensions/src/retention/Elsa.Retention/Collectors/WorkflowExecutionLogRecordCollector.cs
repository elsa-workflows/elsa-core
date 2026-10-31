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
public class WorkflowExecutionLogRecordCollector(IWorkflowExecutionLogStore store) : IRelatedEntityCollector<WorkflowExecutionLogRecord>
{
    public async IAsyncEnumerable<ICollection<WorkflowExecutionLogRecord>> GetRelatedEntities(ICollection<WorkflowInstance> workflowInstances)
    {
        var chunks = workflowInstances.Chunk(25);

        foreach (var chunk in chunks)
        {
            var filter = new WorkflowExecutionLogRecordFilter()
            {
                WorkflowInstanceIds = chunk.Select(x => x.Id).ToArray()
            };

            var pageArgs = PageArgs.FromPage(0, 100);

            while (true)
            {
                var page = await store.FindManyAsync(filter, pageArgs);
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