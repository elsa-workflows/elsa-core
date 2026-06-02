using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.ModularPersistence.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ModularPersistence.ShellFeatures;

[ShellFeature(
    DisplayName = "Modular Persistence",
    Description = "Provides provider-neutral modular persistence manifests, startup materialization, and diagnostics.",
    DependsOn = ["ElsaFastEndpoints"])]
[UsedImplicitly]
public class ModularPersistenceFeature : IFastEndpointsShellFeature
{
    public bool MaterializeOnStartup { get; set; } = true;

    public string? ProviderName { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddModularPersistenceServices(options =>
        {
            options.MaterializeOnStartup = MaterializeOnStartup;
            options.ProviderName = ProviderName;
        });
    }
}
