using Bpmn.Interchange;
using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Bpmn.ShellFeatures;
using Elsa.Common.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.ShellFeatures;

/// <summary>
/// Provides BPMN XML interchange support to the system.
/// </summary>
/// <remarks>
/// The task-to-activity binding this module reads lives inside the BPMN document as an <c>elsa:</c> vendor extension,
/// so an exported <c>.bpmn</c> carries binding configuration — including input expressions — verbatim. Exporting a
/// process discloses its implementation detail; see <see cref="BpmnActivityBindingFormat"/> for the format and for why
/// nothing is redacted.
/// </remarks>
[ShellFeature(
    DisplayName = "BPMN Interchange",
    Description = "Provides BPMN XML interchange (import/export) capabilities for workflows.",
    DependsOn = [typeof(BpmnFeature)])]
[UsedImplicitly]
public class BpmnInterchangeFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<BpmnActivityBindingFormat>();
        services.AddSingleton<BpmnWorkBinder>();
        services.AddSingleton<BpmnXmlReader>();
        services.AddSingleton<BpmnXmlWriter>();
        services.AddScoped<BpmnInterchangeDocumentService>();
    }
}
