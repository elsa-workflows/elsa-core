using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Extensions;
using Elsa.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.StartupTasks;

/// <summary>
/// A startup task that updates the route table with bookmarks. Triggers are already processed at startup.
/// </summary>
public class UpdateRouteTableWithBookmarks : IStartupTask
{
    private readonly IBookmarkFinder _bookmarkFinder;
    private readonly IRouteTable _routeTable;

    public UpdateRouteTableWithBookmarks(IBookmarkFinder bookmarkFinder, IRouteTable routeTable)
    {
        _bookmarkFinder = bookmarkFinder;
        _routeTable = routeTable;
    }

    public int Order => 1000;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Find all HTTP endpoint bookmarks.
        var bookmarks = await _bookmarkFinder.FindBookmarksByTypeAsync<HttpEndpointBookmark>(cancellationToken: cancellationToken).ToList();

        // Add them to the route table.
        _routeTable.AddRoutes(bookmarks);
    }
}