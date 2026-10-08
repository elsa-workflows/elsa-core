using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.AI;

/// <summary>
/// Represents the Weaver AI feature module for the Elsa Studio application.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public class Feature : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.AI.Host.ShellFeatures.AIFeature";
}
