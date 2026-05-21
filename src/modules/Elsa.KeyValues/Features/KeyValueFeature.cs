using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.KeyValues.Features;

/// <summary>
/// Installs and configures instance management features.
/// </summary>
public class KeyValueFeature : FeatureBase
{
    private static readonly Func<IServiceProvider, IKeyValueStore> DefaultKeyValueStore = sp => ActivatorUtilities.CreateInstance<MemoryKeyValueStore>(sp);
    private Func<IServiceProvider, IKeyValueStore> _keyValueStore = DefaultKeyValueStore;
    private bool? _registerMemoryStore;

    /// <inheritdoc />
    public KeyValueFeature(IModule module) : base(module)
    {
    }

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

    /// <inheritdoc />
    public override void Apply()
    {
        if (RegisterMemoryStore)
            Services.TryAddSingleton<MemoryStore<SerializedKeyValuePair>>();

        Services.TryAddScoped<IKeyValueStore>(KeyValueStore);
    }
}
