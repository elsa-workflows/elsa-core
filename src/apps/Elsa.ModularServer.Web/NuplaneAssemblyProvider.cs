using System.Reflection;
using CShells.Features;
using JetBrains.Annotations;
using Nuplane.Loading;

namespace Elsa.ModularServer.Web;

[UsedImplicitly]
internal sealed class NuplaneAssemblyProvider(IPackageAssemblyCatalog packageLoader) : IFeatureAssemblyProvider
{
    public async Task<IEnumerable<Assembly>> GetAssembliesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        return await packageLoader.GetAssembliesAsync(cancellationToken);
    }
}