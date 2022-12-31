using Elsa.Workflows.Sinks.Features;

namespace Elsa.EntityFrameworkCore.Modules.WorkflowSink
{
    public static class Extensions
    {
        public static WorkflowSinkFeature UseEntityFrameworkCore(this WorkflowSinkFeature feature, Action<EFCoreWorkflowSinkPersistenceFeature>? configure = default)
        {
            feature.Module.Configure(configure);
            return feature;
        }
    }
}