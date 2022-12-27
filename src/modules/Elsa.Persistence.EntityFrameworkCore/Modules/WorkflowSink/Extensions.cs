using Elsa.Workflows.Sink.Features;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink
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