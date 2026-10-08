using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Studio.Core.BlazorServer.HostedServices;

/// <summary>
/// Executes all registered <see cref="IStartupTask"/> services.
/// </summary>
public class RunStartupTasksHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="RunStartupTasksHostedService"/>.
    /// </summary>
    /// <param name="serviceScopeFactory"></param>
    public RunStartupTasksHostedService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    
    /// <summary>
    /// Resolves all registered <see cref="IStartupTask"/> services and executes them.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var startupTaskRunner = scope.ServiceProvider.GetRequiredService<IStartupTaskRunner>();
        
        await startupTaskRunner.RunStartupTasksAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}