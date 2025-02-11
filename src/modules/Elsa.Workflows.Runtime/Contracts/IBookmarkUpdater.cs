using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime;

public interface IBookmarkUpdater
{
    Task UpdateBookmarksAsync(UpdateBookmarksRequest request, CancellationToken cancellationToken = default);
}