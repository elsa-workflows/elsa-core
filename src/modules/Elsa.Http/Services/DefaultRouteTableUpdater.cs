using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class DefaultRouteTableUpdater : IRouteTableUpdater
{
    private readonly IRouteTable _routeTable;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRouteTableUpdater"/> class.
    /// </summary>
    public DefaultRouteTableUpdater(IRouteTable routeTable, ITriggerStore triggerStore, IBookmarkStore bookmarkStore)
    {
        _routeTable = routeTable;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var triggerFilter = new TriggerFilter { Name = bookmarkName };
        var bookmarkFilter = new BookmarkFilter { ActivityTypeName = bookmarkName};
        var triggers = (await _triggerStore.FindManyAsync(triggerFilter, cancellationToken)).ToList();
        var bookmarks = (await _bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).ToList();

        _routeTable.AddRoutes(triggers);
        _routeTable.AddRoutes(bookmarks);
    }
}