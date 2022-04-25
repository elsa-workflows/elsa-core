using Elsa.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Runtime.HostedServices;

/// <summary>
/// Asynchronously indexes all workflows in the background.
/// </summary>
public class IndexWorkflowTriggers : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public IndexWorkflowTriggers(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var triggerIndexer = scope.ServiceProvider.GetRequiredService<ITriggerIndexer>();
        await triggerIndexer.IndexTriggersAsync(stoppingToken);
    }
}