using CShells.Features;
using Elsa.Common.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.ShellFeatures;

/// <summary>
/// Provides BPMN execution support to the system.
/// </summary>
[ShellFeature(
    DisplayName = "BPMN",
    Description = "Provides BPMN execution capabilities for workflows.")]
[UsedImplicitly]
public class BpmnFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
