using Elsa.Retention.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Retention.Collectors;

/// <summary>
///     Collects all <see cref="StoredBookmark" /> related to the <see cref="WorkflowInstance" />
/// </summary>
public class BookmarkCollector(IBookmarkStore store) : IRelatedEntityCollector<StoredBookmark>
{
    public async IAsyncEnumerable<ICollection<StoredBookmark>> GetRelatedEntities(ICollection<WorkflowInstance> workflowInstances)
    {
        var batches = workflowInstances.Chunk(25);

        foreach (var batch in batches)
        {
            var filter = new BookmarkFilter()
            {
                WorkflowInstanceIds = batch.Select(x => x.Id).ToArray()
            };

            var bookmarks = await store.FindManyAsync(filter);
            yield return bookmarks.ToArray();
        }
    }
}