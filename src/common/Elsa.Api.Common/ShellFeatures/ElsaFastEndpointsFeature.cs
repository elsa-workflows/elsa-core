using CShells.FastEndpoints.Contracts;
using CShells.Features;
using Elsa.FastEndpointConfigurators;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ShellFeatures;

/// <summary>
/// Registers the Elsa-specific FastEndpoints configurator.
/// This feature should be enabled alongside the CShells FastEndpointsFeature to apply Elsa's serialization settings.
/// </summary>
[ShellFeature(DependsOn = ["FastEndpoints"])]
[UsedImplicitly]
public class ElsaFastEndpointsFeature : IShellFeature
{
    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFastEndpointsConfigurator, ElsaFastEndpointsConfigurator>();
    }
}
