using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
/// </summary>
public class PopulateRegistriesHostedService : IHostedService
{
    private readonly IRegistriesPopulator _registriesPopulator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulateRegistriesHostedService"/> class.
    /// </summary>
    public PopulateRegistriesHostedService(IRegistriesPopulator registriesPopulator)
    {
        _registriesPopulator = registriesPopulator;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken) => await _registriesPopulator.PopulateAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}