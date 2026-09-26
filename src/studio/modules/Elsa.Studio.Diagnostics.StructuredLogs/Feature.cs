using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.Diagnostics.StructuredLogs;

/// <summary>
/// Represents the diagnostics structured logs feature module for the Elsa Studio application.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public class Feature : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogs";
}
