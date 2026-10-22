using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.Bpmn.Extensions;

/// <summary>Registers BPMN designers with the host.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables <paramref name="designer"/>. A designer module calls this from its own registration method, such as
    /// <c>AddBpmnJsDesigner</c>; a host enables a designer by calling that. Enabling the same designer again has no
    /// effect.
    /// </summary>
    public static IServiceCollection AddBpmnDesigner(this IServiceCollection services, BpmnDesignerDescriptor designer)
    {
        services.AddBpmnDesignerResolver();

        if (!services.Any(service => service.ServiceType == typeof(BpmnDesignerDescriptor) && Equals(service.ImplementationInstance, designer)))
            services.AddSingleton(designer);

        return services;
    }

    /// <summary>
    /// Registers the <see cref="BpmnDesignerResolver"/> and the check that makes an ambiguous designer choice fail at
    /// startup. Callers that only need to ask which designer is enabled, such as the workflow editor, call this; it is
    /// safe to call more than once.
    /// </summary>
    public static IServiceCollection AddBpmnDesignerResolver(this IServiceCollection services)
    {
        services.AddOptions<BpmnDesignerOptions>();
        services.TryAddSingleton<BpmnDesignerResolver>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupTask, BpmnDesignerSelectionCheck>());
        return services;
    }
}
