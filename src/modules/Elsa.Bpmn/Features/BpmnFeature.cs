using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Bpmn.Features;

/// <summary>
/// Provides BPMN execution support to the system.
/// </summary>
public class BpmnFeature : FeatureBase
{
    /// <inheritdoc />
    public BpmnFeature(IModule module) : base(module)
    {
    }
}
