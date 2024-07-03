using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities.Flowchart.Serialization;

namespace Elsa.Workflows.Features;

/// <summary>
/// Adds support for the Flowchart activity.
/// </summary>
public class FlowchartFeature : FeatureBase
{
    /// <inheritdoc />
    public FlowchartFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSerializationOptionsConfigurator<FlowchartSerializationOptionConfigurator>();
        
    }

    public override void Configure()
    {
        Module.AddTypeAlias<FlowScope>("FlowScope");
    }
}