using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Constants;

namespace Elsa.Studio.Alterations;

/// <summary>
/// The alterations feature module — a designer for staging and submitting alteration plans
/// against running workflow instances.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public class Feature : FeatureBase
{
    /// <summary>
    /// The backend remote feature name required by this module.
    /// </summary>
    public const string RemoteFeatureName = RemoteFeatureNames.Alterations;
}
