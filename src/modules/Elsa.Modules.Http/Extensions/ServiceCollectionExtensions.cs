using Elsa.Mediator.Extensions;
using Elsa.Modules.Http.Handlers;
using Elsa.Modules.Http.Implementations;
using Elsa.Modules.Http.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpActivityServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IRouteMatcher, RouteMatcher>()
            .AddSingleton<IRouteTable, RouteTable>()
            .AddNotificationHandlersFrom<UpdateRouteTable>()
            .AddHttpContextAccessor();
    }
}