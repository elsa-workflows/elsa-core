using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Http.HostedServices;

/// <summary>
/// Update the route table based on workflow triggers and bookmarks.
/// </summary>
public class UpdateRouteTableHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRouteTableHostedService"/> class.
    /// </summary>
    public UpdateRouteTableHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var routeTableUpdater = scope.ServiceProvider.GetRequiredService<IRouteTableUpdater>();
        await routeTableUpdater.UpdateAsync(stoppingToken);
    }
}