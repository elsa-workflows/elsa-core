using Bpmn.Interchange;
using Elsa.Bpmn.Features;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Handlers.Notifications;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.Features;

/// <summary>
/// Provides BPMN XML interchange support to the system.
/// </summary>
/// <remarks>
/// The task-to-activity binding this module reads lives inside the BPMN document as an <c>elsa:</c> vendor extension,
/// so an exported <c>.bpmn</c> carries binding configuration — including input expressions — verbatim. Exporting a
/// process discloses its implementation detail; see <see cref="BpmnActivityBindingFormat"/> for the format and for why
/// nothing is redacted.
/// </remarks>
[DependsOn(typeof(BpmnFeature))]
public class BpmnInterchangeFeature : FeatureBase
{
    /// <inheritdoc />
    public BpmnInterchangeFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<BpmnInterchangeFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<BpmnActivityBindingFormat>();
        Services.AddSingleton<BpmnWorkBinder>();
        Services.AddSingleton<BpmnXmlReader>();
        Services.AddSingleton<BpmnXmlWriter>();
        Services.AddScoped<BpmnInterchangeDocumentService>();
        Services.AddNotificationHandler<ValidateBpmnProcessBindings>();
    }
}
