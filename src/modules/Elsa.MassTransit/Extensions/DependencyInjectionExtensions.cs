using Elsa.MassTransit.Implementations;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddMassTransitServices(this IServiceCollection services)
    {
        return services.AddSingleton<IWorkflowDispatcher, MassTransitWorkflowDispatcher>();
    }
}