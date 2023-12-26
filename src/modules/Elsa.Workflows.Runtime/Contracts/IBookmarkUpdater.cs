using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Contracts;

public interface IBookmarkUpdater
{
    Task UpdateBookmarksAsync(UpdateBookmarksRequest request, CancellationToken cancellationToken = default);
}