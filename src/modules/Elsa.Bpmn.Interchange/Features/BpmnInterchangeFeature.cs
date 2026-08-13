using Elsa.Bpmn.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;

namespace Elsa.Bpmn.Interchange.Features;

/// <summary>
/// Provides BPMN XML interchange support to the system.
/// </summary>
[DependsOn(typeof(BpmnFeature))]
public class BpmnInterchangeFeature : FeatureBase
{
    /// <inheritdoc />
    public BpmnInterchangeFeature(IModule module) : base(module)
    {
    }
}
