using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Http.Options;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class DefaultRouteTableUpdater(IRouteTable routeTable, ITriggerStore triggerStore, IBookmarkStore bookmarkStore, IOptions<HttpActivityOptions> options)
    : IRouteTableUpdater
{
    /// <inheritdoc />
    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var triggerFilter = new TriggerFilter
        {
            Name = bookmarkName,
            TenantAgnostic = true
        };
        var bookmarkFilter = new BookmarkFilter
        {
            ActivityTypeName = bookmarkName,
            TenantAgnostic = true
        };
        var triggers = (await triggerStore.FindManyAsync(triggerFilter, cancellationToken)).ToList();
        var bookmarks = (await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).ToList();

        routeTable.AddRoutes(triggers, options);
        routeTable.AddRoutes(bookmarks);
    }
}