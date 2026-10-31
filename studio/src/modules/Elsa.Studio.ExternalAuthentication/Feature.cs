using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.ExternalAuthentication;

/// <summary>
/// Enables the External Authentication Studio experience when the server exposes the capability.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public sealed class Feature : FeatureBase
{
    /// <summary>The matching Elsa Server shell feature name.</summary>
    public const string RemoteFeatureName = "Elsa.ExternalAuthentication.ShellFeatures.ExternalAuthentication";
}
