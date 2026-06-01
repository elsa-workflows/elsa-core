using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.ModularPersistence.Features;

/// <summary>
/// Registers provider-neutral modular persistence manifests and startup materialization.
/// </summary>
public sealed class ModularPersistenceFeature(IModule module) : FeatureBase(module)
{
    public bool MaterializeOnStartup { get; set; } = true;

    public ModularPersistenceFeature RegisterManifest(StorageManifestDescriptor manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        Services.AddSingleton(new StorageManifestRegistration(manifest));
        return this;
    }

    public override void Apply()
    {
        Services.TryAddSingleton<IStorageManifestRegistry, StorageManifestRegistry>();
        Services.Configure<ModularPersistenceOptions>(options => options.MaterializeOnStartup = MaterializeOnStartup);
        Services.AddStartupTask<ModularPersistenceMaterializationStartupTask>();
    }
}
