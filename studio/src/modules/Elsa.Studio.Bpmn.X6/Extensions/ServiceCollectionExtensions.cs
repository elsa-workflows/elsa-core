using Elsa.Studio.Bpmn.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Bpmn.X6.Extensions;

/// <summary>Enables the X6 BPMN designer.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Enables the X6 BPMN designer. Its script loads only when a BPMN canvas opens.</summary>
    public static IServiceCollection AddX6BpmnDesigner(this IServiceCollection services) => services.AddBpmnDesigner(X6BpmnDesigner.Descriptor);
}
