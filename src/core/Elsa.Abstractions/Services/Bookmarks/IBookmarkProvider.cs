using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IBookmarkProvider
    {
        ValueTask<bool> SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default);
        ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default);
    }
}