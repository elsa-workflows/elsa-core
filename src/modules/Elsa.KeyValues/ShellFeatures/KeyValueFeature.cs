using CShells.Features;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Stores;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

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
    /// <summary>
    /// A factory that instantiates an <see cref="IKeyValueStore"/>.
    /// </summary>
    public Func<IServiceProvider, IKeyValueStore> KeyValueStore { get; set; } = sp => ActivatorUtilities.CreateInstance<MemoryKeyValueStore>(sp);

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped(KeyValueStore);
    }
}
