using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Management.ShellFeatures;

/// <summary>
/// Shell feature for secrets management.
/// </summary>
[ShellFeature(
    DisplayName = "Secrets Management",
    Description = "Provides secrets management APIs and functionality",
    DependsOn = ["SecretsCore"])]
[UsedImplicitly]
public class SecretManagementShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }
}

