using CShells.Features;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Options;
using Elsa.Workflows.Activities.Flowchart.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ShellFeatures;

/// <summary>
/// Adds support for the Flowchart activity.
/// </summary>
[ShellFeature(
    DisplayName = "Flowchart",
    Description = "Adds support for the Flowchart activity")]
[UsedImplicitly]
public class FlowchartFeature : IShellFeature
{
    /// <summary>
    /// A delegate to configure <see cref="FlowchartOptions"/>.
    /// </summary>
    public Action<FlowchartOptions>? FlowchartOptionsConfigurator { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSerializationOptionsConfigurator<FlowchartSerializationOptionConfigurator>();

        // Register FlowchartOptions
        services.AddOptions<FlowchartOptions>();

        if (FlowchartOptionsConfigurator != null)
            services.Configure(FlowchartOptionsConfigurator);
    }
}

