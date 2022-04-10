using Elsa.Modules.MassTransit.Services;
using Elsa.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Modules.MassTransit.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMassTransitServices(this IServiceCollection services)
    {
        return services.AddSingleton<IWorkflowDispatcher, MassTransitWorkflowDispatcher>();
    }
}