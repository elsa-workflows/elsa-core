using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DevOps.GitHub.ShellFeatures;

/// <summary>
/// Shell feature for GitHub DevOps integration.
/// </summary>
[ShellFeature(
    DisplayName = "GitHub DevOps",
    Description = "Enables GitHub integration for DevOps activities")]
[UsedImplicitly]
public class GitHubDevOpsShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }
}

