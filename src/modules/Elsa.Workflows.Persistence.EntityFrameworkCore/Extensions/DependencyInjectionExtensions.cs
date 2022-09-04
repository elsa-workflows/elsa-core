using Elsa.Features.Services;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Features;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule UseEntityFrameworkCorePersistence(this IModule module, Action<EFCoreWorkflowPersistenceFeature> configure)
    {
        module.Configure(configure);
        return module;
    }
}