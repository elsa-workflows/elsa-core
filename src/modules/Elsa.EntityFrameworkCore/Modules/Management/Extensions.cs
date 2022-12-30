using Elsa.Workflows.Management.Features;

namespace Elsa.EntityFrameworkCore.Modules.Management
{
    public static class Extensions
    {
        public static WorkflowManagementFeature UseEntityFrameworkCore(this WorkflowManagementFeature feature, Action<EFCoreManagementPersistenceFeature>? configure = default)
        {
            feature.Module.Configure(configure);
            return feature;
        }
    }
}