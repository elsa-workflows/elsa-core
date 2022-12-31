using Elsa.Features.Services;
using Elsa.MassTransit.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule AddMassTransitWorkflowDispatchers(this IModule module)
    {
        module.Configure<MassTransitDispatchersFeature>();
        return module;
    }
}