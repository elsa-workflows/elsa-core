using CShells.Features;
using Elsa.Bpmn.ShellFeatures;
using Elsa.Common.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.ShellFeatures;

/// <summary>
/// Provides BPMN XML interchange support to the system.
/// </summary>
[ShellFeature(
    DisplayName = "BPMN Interchange",
    Description = "Provides BPMN XML interchange (import/export) capabilities for workflows.",
    DependsOn = [typeof(BpmnFeature)])]
[UsedImplicitly]
public class BpmnInterchangeFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
