using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.Security;

/// <summary>
/// Represents the security feature module for the Elsa Studio application.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public class Feature : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Identity.ShellFeatures.Identity";
}
