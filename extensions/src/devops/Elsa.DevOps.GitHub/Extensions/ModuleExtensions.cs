using Elsa.Features.Services;
using Elsa.DevOps.GitHub.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> with methods to use GitHub integration.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the GitHub API feature.
    /// </summary>
    public static IModule UseGitHub(this IModule module, Action<GitHubFeature>? configure = null)
    {
        return module.Use(configure);
    }
}