using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Dashboard.Widgets;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardWidget<TComponent>(
        this IServiceCollection services,
        string id,
        string zone,
        int order,
        string? title = null,
        string? requiredBackendCapability = null,
        string? payloadKind = null)
        where TComponent : IComponent
    {
        return services.AddScoped(_ => new DashboardWidgetDescriptor(
            id,
            zone,
            order,
            typeof(TComponent),
            title,
            requiredBackendCapability,
            payloadKind));
    }
}
