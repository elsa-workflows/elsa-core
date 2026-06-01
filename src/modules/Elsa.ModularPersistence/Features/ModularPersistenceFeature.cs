using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Extensions;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ModularPersistence.Features;

/// <summary>
/// Registers provider-neutral modular persistence manifests and startup materialization.
/// </summary>
public sealed class ModularPersistenceFeature(IModule module) : FeatureBase(module)
{
    public bool MaterializeOnStartup { get; set; } = true;

    public string? ProviderName { get; set; }

    public Action<ModularPersistenceOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<ModularPersistenceFeature>();
    }

    public ModularPersistenceFeature RegisterManifest(StorageManifestDescriptor manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        Services.AddSingleton(new StorageManifestRegistration(manifest));
        return this;
    }

    public ModularPersistenceFeature UseProvider(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ProviderName = providerName;
        return this;
    }

    public override void Apply()
    {
        Services.AddModularPersistenceServices(options =>
        {
            options.MaterializeOnStartup = MaterializeOnStartup;
            options.ProviderName = ProviderName;
            ConfigureOptions?.Invoke(options);
        });
        Module.AddFastEndpointsFromModule();
    }
}
