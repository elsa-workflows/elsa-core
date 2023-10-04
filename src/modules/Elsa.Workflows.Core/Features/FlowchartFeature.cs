using Elsa.Common.Contracts;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Activities.Flowchart.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Features;

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
        Services.AddSingleton<ISerializationOptionsConfigurator, FlowchartSerializationOptionConfigurator>();
    }
}