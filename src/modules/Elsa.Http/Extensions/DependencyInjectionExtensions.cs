using Elsa.Http.Handlers;
using Elsa.Http.Implementations;
using Elsa.Http.Services;
using Elsa.Mediator.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
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