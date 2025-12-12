using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities.Flowchart.Options;
using Elsa.Workflows.Activities.Flowchart.Serialization;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// A delegate to configure <see cref="FlowchartOptions"/>.
    /// </summary>
    public Action<FlowchartOptions>? FlowchartOptionsConfigurator { get; set; }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSerializationOptionsConfigurator<FlowchartSerializationOptionConfigurator>();

        // Register FlowchartOptions
        Services.AddOptions<FlowchartOptions>();

        if (FlowchartOptionsConfigurator != null)
            Services.Configure(FlowchartOptionsConfigurator);
    }

    public override void Configure()
    {
        Module.AddTypeAlias<FlowScope>("FlowScope");
    }
}