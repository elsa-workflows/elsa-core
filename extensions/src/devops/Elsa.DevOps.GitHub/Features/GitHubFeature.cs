using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.DevOps.GitHub.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DevOps.GitHub.Features;

/// <summary>
/// Represents a feature for setting up GitHub integration within the Elsa framework.
/// </summary>
public class GitHubFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Applies the feature to the specified service collection.
    /// </summary>
    public override void Apply() =>
        Services
            .AddSingleton<GitHubClientFactory>();
}