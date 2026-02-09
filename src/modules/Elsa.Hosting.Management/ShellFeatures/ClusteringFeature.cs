using CShells.Features;
using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Hosting.Management.ShellFeatures;

/// <summary>
/// Installs and configures the clustering feature.
/// </summary>
[ShellFeature(
    DisplayName = "Clustering",
    Description = "Provides application clustering and heartbeat monitoring capabilities")]
public class ClusteringFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IApplicationInstanceNameProvider, RandomApplicationInstanceNameProvider>()
            .AddSingleton<RandomIntIdentityGenerator>();
    }
}
