using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.KeyValues.Features;

/// <summary>
/// Installs and configures instance management features.
/// </summary>
public class KeyValueFeature : FeatureBase
{
    /// <inheritdoc />
    public KeyValueFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A factory that instantiates an <see cref="IKeyValueStore"/>.
    /// </summary>
    public Func<IServiceProvider, IKeyValueStore> KeyValueStore { get; set; } = sp => ActivatorUtilities.CreateInstance<MemoryKeyValueStore>(sp);


    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped(KeyValueStore);
    }
}