using CShells.Features;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.KeyValues.ShellFeatures;

/// <summary>
/// Installs and configures instance management features.
/// </summary>
[ShellFeature(
    DisplayName = "Key-Value Store",
    Description = "Provides key-value storage capabilities")]
public class KeyValueFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IKeyValueStore, MemoryKeyValueStore>();
    }
}
