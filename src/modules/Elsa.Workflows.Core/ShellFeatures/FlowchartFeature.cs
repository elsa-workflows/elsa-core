using CShells.Features;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities.Flowchart.Options;
using Elsa.Workflows.Activities.Flowchart.Serialization;
using Elsa.Workflows.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Common.Serialization;
using Elsa.Platform.PackageManifest.Generator.Hints;

namespace Elsa.Workflows.ShellFeatures;

/// <summary>
/// Adds support for the Flowchart activity.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Workflows)]
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

        services.Configure<SerializationTypeOptions>(options => options.AddTypeAlias<FlowScope>("FlowScope"));
    }
}
