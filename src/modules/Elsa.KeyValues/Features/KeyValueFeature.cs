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

    /// <inheritdoc />
    public KeyValueFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A factory that instantiates an <see cref="IKeyValueStore"/>.
    /// </summary>
    public Func<IServiceProvider, IKeyValueStore> KeyValueStore { get; set; } = DefaultKeyValueStore;

    /// <inheritdoc />
    public override void Apply()
    {
        if (KeyValueStore == DefaultKeyValueStore && !Services.Any(x => x.ServiceType == typeof(IKeyValueStore)))
            Services.TryAddSingleton<MemoryStore<SerializedKeyValuePair>>();

        Services.TryAddScoped<IKeyValueStore>(KeyValueStore);
    }
}
