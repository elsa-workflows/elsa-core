using Elsa.Retention.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Retention.Collectors;

/// <summary>
///     Collects all <see cref="StoredBookmark" /> related to the <see cref="WorkflowInstance" />
/// </summary>
public class BookmarkCollector : IRelatedEntityCollector<StoredBookmark>
{
    private readonly IBookmarkStore _store;

    public BookmarkCollector(IBookmarkStore store)
    {
        _store = store;
    }

    public async IAsyncEnumerable<ICollection<StoredBookmark>> GetRelatedEntities(ICollection<WorkflowInstance> workflowInstances)
    {
        IEnumerable<WorkflowInstance[]> batches = workflowInstances.Chunk(25);

        foreach (WorkflowInstance[] batch in batches)
        {
            BookmarkFilter filter = new()
            {
                WorkflowInstanceIds = batch.Select(x => x.Id).ToArray()
            };

            IEnumerable<StoredBookmark> bookmarks = await _store.FindManyAsync(filter);
            yield return bookmarks.ToArray();
        }
    }
}