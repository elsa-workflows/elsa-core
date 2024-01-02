using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
/// </summary>
public class PopulateRegistriesHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulateRegistriesHostedService"/> class.
    /// </summary>
    public PopulateRegistriesHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var registriesPopulator = scope.ServiceProvider.GetRequiredService<IRegistriesPopulator>();
        await registriesPopulator.PopulateAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}