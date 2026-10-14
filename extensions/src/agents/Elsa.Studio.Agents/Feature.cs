using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using JetBrains.Annotations;

namespace Elsa.Studio.Agents;

[UsedImplicitly]
[RemoteFeature(RemoteFeatureName)]
public class Feature : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.AgentsApi";
}
