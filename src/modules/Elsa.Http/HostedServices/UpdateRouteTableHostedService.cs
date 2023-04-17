using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Http.HostedServices;

/// <summary>
/// Update the route table based on workflow triggers and bookmarks.
/// </summary>
public class UpdateRouteTableHostedService : BackgroundService
{
    private readonly IRouteTable _routeTable;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRouteTableHostedService"/> class.
    /// </summary>
    public UpdateRouteTableHostedService(IRouteTable routeTable, ITriggerStore triggerStore, IBookmarkStore bookmarkStore)
    {
        _routeTable = routeTable;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var triggerFilter = new TriggerFilter { Name = bookmarkName };
        var bookmarkFilter = new BookmarkFilter { ActivityTypeName = bookmarkName};
        var triggers = (await _triggerStore.FindManyAsync(triggerFilter, stoppingToken)).ToList();
        var bookmarks = (await _bookmarkStore.FindManyAsync(bookmarkFilter, stoppingToken)).ToList();

        _routeTable.AddRoutes(triggers);
        _routeTable.AddRoutes(bookmarks);
    }
}