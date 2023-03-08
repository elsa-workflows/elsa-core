using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Synchronously updates the routetable from the triggers.
/// </summary>
public class PopulateRouteTable : IHostedService
{
    private readonly ITriggerIndexer _triggerIndexer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PopulateRouteTable(ITriggerIndexer triggerIndexer)
    {
        _triggerIndexer = triggerIndexer;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _triggerIndexer.IndexAllTriggersAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}