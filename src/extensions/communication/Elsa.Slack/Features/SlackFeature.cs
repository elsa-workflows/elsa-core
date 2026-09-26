using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Slack.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Slack.Features;

/// <summary>
/// Represents a feature for setting up Slack integration within the Elsa framework.
/// </summary>
public class SlackFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Applies the feature to the specified service collection.
    /// </summary>
    public override void Apply() =>
        Services
            .AddSingleton<SlackClientFactory>();
}