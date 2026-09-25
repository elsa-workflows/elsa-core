using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.Diagnostics.OpenTelemetry;

/// <summary>
/// Represents the diagnostics OpenTelemetry feature module for the Elsa Studio application.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public class Feature : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Diagnostics.OpenTelemetry.ShellFeatures.OpenTelemetry";
}
