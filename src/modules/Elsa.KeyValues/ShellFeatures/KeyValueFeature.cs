using CShells.Features;
using Elsa.Common.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Stores;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.KeyValues.ShellFeatures;

/// <summary>
/// Installs and configures key-value store features.
/// </summary>
[ShellFeature(
    DisplayName = "Key-Value Store",
    Description = "Provides key-value storage capabilities for workflows")]
[UsedImplicitly]
public class KeyValueFeature : IShellFeature
{
    private static readonly Func<IServiceProvider, IKeyValueStore> DefaultKeyValueStore = sp => ActivatorUtilities.CreateInstance<MemoryKeyValueStore>(sp);

    /// <summary>
    /// A factory that instantiates an <see cref="IKeyValueStore"/>.
    /// </summary>
    public Func<IServiceProvider, IKeyValueStore> KeyValueStore { get; set; } = DefaultKeyValueStore;

    public void ConfigureServices(IServiceCollection services)
    {
        if (KeyValueStore == DefaultKeyValueStore && !services.Any(x => x.ServiceType == typeof(IKeyValueStore)))
            services.TryAddSingleton<MemoryStore<SerializedKeyValuePair>>();

        services.TryAddScoped<IKeyValueStore>(KeyValueStore);
    }
}
