using Elsa.Workflows.Runtime.Distributed.Features;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Workflows.Runtime.Distributed.Extensions;

public static class ModuleExtensions
{
    public static WorkflowRuntimeFeature UseDistributedRuntime(this WorkflowRuntimeFeature feature, Action<DistributedRuntimeFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}