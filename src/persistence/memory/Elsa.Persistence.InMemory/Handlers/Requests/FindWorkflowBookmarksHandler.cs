using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindWorkflowBookmarksHandler : IRequestHandler<FindWorkflowBookmarks, IEnumerable<WorkflowBookmark>>
{
    private readonly InMemoryStore<WorkflowBookmark> _store;

    public FindWorkflowBookmarksHandler(InMemoryStore<WorkflowBookmark> store)
    {
        _store = store;
    }

    public Task<IEnumerable<WorkflowBookmark>> HandleAsync(FindWorkflowBookmarks request, CancellationToken cancellationToken)
    {
        var bookmarks = Find(request);
        return Task.FromResult(bookmarks);
    }

    private IEnumerable<WorkflowBookmark> Find(FindWorkflowBookmarks request) => request.WorkflowInstanceId != null
        ? FindByWorkflowInstanceId(request.WorkflowInstanceId)
        : request.Name != null
            ? FindByName(request.Name, request.Hash)
            : Enumerable.Empty<WorkflowBookmark>();

    private IEnumerable<WorkflowBookmark> FindByWorkflowInstanceId(string instanceId) => _store.FindMany(x => x.WorkflowInstanceId == instanceId).ToList();

    private IEnumerable<WorkflowBookmark> FindByName(string name, string? hash)
    {
        return _store.Query(query =>
        {
            query = query.Where(x => x.Name == name);

            if (!string.IsNullOrWhiteSpace(hash))
                query = query.Where(x => x.Hash == hash);

            return query;
        });
    }
}