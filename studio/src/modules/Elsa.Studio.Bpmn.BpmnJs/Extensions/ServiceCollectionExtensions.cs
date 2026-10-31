using Elsa.Studio.Bpmn.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Bpmn.BpmnJs.Extensions;

/// <summary>Enables the bpmn-js BPMN designer.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Enables the bpmn-js BPMN designer. Its script loads only when a BPMN canvas opens.</summary>
    public static IServiceCollection AddBpmnJsDesigner(this IServiceCollection services) => services.AddBpmnDesigner(BpmnJsDesigner.Descriptor);
}
