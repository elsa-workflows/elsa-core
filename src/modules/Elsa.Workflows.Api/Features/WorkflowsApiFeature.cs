using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Features;
using Elsa.JavaScript.Features;
using Elsa.Workflows.Api.Serialization;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Features;

/// <summary>
/// Adds workflows API features.
/// </summary>
[DependsOn(typeof(WorkflowInstancesFeature))]
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(JavaScriptFeature))]
[DependsOn(typeof(HttpFeature))]
public class WorkflowsApiFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowsApiFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<ISerializationOptionsConfigurator, SerializationConfigurator>();
        Module.AddFastEndpointsFromModule();
    }
}