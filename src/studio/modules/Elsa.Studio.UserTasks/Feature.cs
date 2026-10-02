using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.UserTasks;

/// <summary>
/// Registers the remote User Tasks shell feature.
/// </summary>
[RemoteFeature(RemoteFeatureName)]
public sealed class Feature : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.UserTasks.ShellFeatures.UserTasks";
}
