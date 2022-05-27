using Elsa.Workflows.Persistence.EntityFrameworkCore.Features;
using Elsa.Workflows.Persistence.Features;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowPersistenceFeature UseEntityFrameworkCore(this WorkflowPersistenceFeature feature, Action<EFCoreWorkflowPersistenceFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}