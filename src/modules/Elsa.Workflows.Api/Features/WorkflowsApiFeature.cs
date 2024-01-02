using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Features;
using Elsa.SasTokens.Features;
using Elsa.Workflows.Api.Serialization;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Features;

/// <summary>
/// Adds workflows API features.
/// </summary>
[DependsOn(typeof(WorkflowInstancesFeature))]
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(HttpFeature))]
[DependsOn(typeof(SasTokensFeature))]
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
        Services.AddScoped<ISerializationOptionsConfigurator, SerializationConfigurator>();
        Module.AddFastEndpointsFromModule();
    }
}