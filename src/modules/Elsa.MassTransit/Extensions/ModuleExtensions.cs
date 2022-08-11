using Elsa.Features.Services;
using Elsa.MassTransit.Features;

namespace Elsa.MassTransit.Extensions;

public static class ModuleExtensions
{
    public static IModule AddMassTransitWorkflowDispatchers(this IModule module)
    {
        module.Configure<MassTransitDispatchersFeature>();
        return module;
    }
}