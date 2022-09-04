using Elsa.Workflows.Runtime.Features;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Runtime
{
    public static class Extensions
    {
        public static WorkflowRuntimeFeature UseEntityFrameworkCore(this WorkflowRuntimeFeature feature, Action<EFCoreRuntimePersistenceFeature>? configure = default)
        {
            feature.Module.Configure(configure);
            return feature;
        }
    }
}