using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities.Flowchart.Options;
using Elsa.Workflows.Activities.Flowchart.Serialization;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Common.Serialization;

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

        Services.Configure<SerializationTypeOptions>(options => options.AddTypeAlias<FlowScope>("FlowScope"));
    }
}
