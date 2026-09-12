using CShells.Features;
using Elsa.Common.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Stores;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.KeyValues.ShellFeatures;

/// <summary>
/// Installs and configures key-value store features.
/// </summary>
[ManifestFeatureCategory("Storage")]
[ManifestFeatureCategory("Infrastructure")]
[ShellFeature(
    DisplayName = "Key-Value Store",
    Description = "Provides key-value storage capabilities for workflows")]
[UsedImplicitly]
public class KeyValueFeature : IShellFeature
{
    private static readonly Func<IServiceProvider, IKeyValueStore> DefaultKeyValueStore = sp => ActivatorUtilities.CreateInstance<MemoryKeyValueStore>(sp);
    private Func<IServiceProvider, IKeyValueStore> _keyValueStore = DefaultKeyValueStore;
    private bool? _registerMemoryStore;

    /// <summary>
    /// A factory that instantiates an <see cref="IKeyValueStore"/>.
    /// </summary>
    public Func<IServiceProvider, IKeyValueStore> KeyValueStore
    {
        get => _keyValueStore;
        set => _keyValueStore = value;
    }

    /// <summary>
    /// Whether to register the in-memory backing store used by <see cref="MemoryKeyValueStore"/>.
    /// </summary>
    public bool RegisterMemoryStore
    {
        get => _registerMemoryStore ?? ReferenceEquals(KeyValueStore, DefaultKeyValueStore);
        set => _registerMemoryStore = value;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        if (RegisterMemoryStore)
            services.TryAddSingleton<MemoryStore<SerializedKeyValuePair>>();

        services.AddScoped<IKeyValueStore>(KeyValueStore);
    }
}
