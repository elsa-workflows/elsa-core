using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.Diagnostics.ConsoleLogs;

/// <summary>
/// Represents the diagnostics console logs feature module for the Elsa Studio application.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public class Feature : FeatureBase
{
    /// <summary>
    /// The backend remote feature name required by this module.
    /// </summary>
    public const string RemoteFeatureName = "Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogs";
}
