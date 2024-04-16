using Elsa.Dsl.Contracts;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.HostedServices;

/// <summary>
/// Registers function activity descriptors with the DSL.
/// </summary>
public class MapActivityDslFunctionsHostedService(IServiceScopeFactory scopeFactory, IOptions<DslIntegrationOptions> options) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var functionActivityRegistry = scope.ServiceProvider.GetRequiredService<IFunctionActivityRegistry>();
        var descriptors = options.Value.FunctionActivityDescriptors;

        foreach (var descriptor in descriptors.Values)
            functionActivityRegistry.RegisterFunction(descriptor);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}