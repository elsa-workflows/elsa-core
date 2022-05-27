using Elsa.Features.Services;
using Elsa.Workflows.Persistence.Features;

namespace Elsa.Workflows.Persistence.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule UseWorkflowPersistence(this IModule module, Action<WorkflowPersistenceFeature>? configure = default )
    {
        module.Configure(configure);
        return module;
    }
}