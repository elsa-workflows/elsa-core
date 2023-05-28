using Elsa.Dsl.Features;
using Elsa.Dsl.Models;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.HostedServices;
using Elsa.Workflows.Management.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Installs JavaScript activities.
/// </summary>
[DependsOn(typeof(DslFeature))]
[PublicAPI]
public class DslIntegrationFeature : FeatureBase
{
    private readonly IDictionary<string, FunctionActivityDescriptor> _dictionary = new Dictionary<string, FunctionActivityDescriptor>();
    
    /// <inheritdoc />
    public DslIntegrationFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// Maps a function to an activity.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="activityTypeName">The name of the activity type.</param>
    /// <param name="propertyNames">The names of the properties that are mapped to the function arguments.</param>
    /// <param name="configure">An optional action that can be used to configure the activity.</param>
    public void MapActivityFunction(string functionName, string activityTypeName, IEnumerable<string>? propertyNames = default, Action<IActivity>? configure = default)
    {
        _dictionary.Add(functionName, new FunctionActivityDescriptor(functionName, activityTypeName, propertyNames, configure));
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        ConfigureHostedService<MapActivityDslFunctionsHostedService>(-1);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure<DslIntegrationOptions>(options =>
        {
            options.FunctionActivityDescriptors = _dictionary;
        });
    }
}