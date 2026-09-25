using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Scripting.ShellFeatures;

/// <summary>
/// Shell feature for secrets scripting support.
/// </summary>
[ShellFeature(
    DisplayName = "Secrets Scripting",
    Description = "Enables access to secrets in workflow expressions",
    DependsOn = ["SecretsCore"])]
[UsedImplicitly]
public class SecretsScriptingShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }
}

