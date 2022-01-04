using Elsa.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime.HostedServices;

public class IndexWorkflowTriggersHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger _logger;

    public IndexWorkflowTriggersHostedService(IServiceScopeFactory serviceScopeFactory, ILogger<IndexWorkflowTriggersHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var triggerIndexer = scope.ServiceProvider.GetRequiredService<ITriggerIndexer>();
        await triggerIndexer.IndexTriggersAsync(cancellationToken);
    }
}