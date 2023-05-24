using Elsa.Http.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Http.HostedServices;

/// <summary>
/// Update the route table based on workflow triggers and bookmarks.
/// </summary>
public class UpdateRouteTableHostedService : BackgroundService
{
    private readonly IRouteTableUpdater _routeTableUpdater;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRouteTableHostedService"/> class.
    /// </summary>
    public UpdateRouteTableHostedService(IRouteTableUpdater routeTableUpdater)
    {
        _routeTableUpdater = routeTableUpdater;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _routeTableUpdater.UpdateAsync(stoppingToken);
    }
}