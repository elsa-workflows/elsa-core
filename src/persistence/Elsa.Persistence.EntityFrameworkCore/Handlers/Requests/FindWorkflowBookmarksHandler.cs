using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowBookmarksHandler : IRequestHandler<FindWorkflowBookmarks, IEnumerable<WorkflowBookmark>>
{
    private readonly IStore<WorkflowBookmark> _store;
    public FindWorkflowBookmarksHandler(IStore<WorkflowBookmark> store) => _store = store;

    public async Task<IEnumerable<WorkflowBookmark>> HandleAsync(FindWorkflowBookmarks request, CancellationToken cancellationToken) => await FindAsync(request, cancellationToken);

    private async Task<IEnumerable<WorkflowBookmark>> FindAsync(FindWorkflowBookmarks request, CancellationToken cancellationToken) => request.WorkflowInstanceId != null
        ? await FindByWorkflowInstanceIdAsync(request.WorkflowInstanceId, cancellationToken)
        : request.Name != null
            ? await FindByNameAsync(request.Name, request.Hash, cancellationToken)
            : Enumerable.Empty<WorkflowBookmark>();

    private async Task<IEnumerable<WorkflowBookmark>> FindByWorkflowInstanceIdAsync(string instanceId, CancellationToken cancellationToken) => await _store.FindManyAsync(x => x.WorkflowInstanceId == instanceId, cancellationToken);
    private async Task<IEnumerable<WorkflowBookmark>> FindByNameAsync(string name, string? hash, CancellationToken cancellationToken) => await _store.FindManyAsync(x => x.Name == name && x.Hash == hash, cancellationToken);
}