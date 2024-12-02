using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;

namespace Elsa.Tenants.Features;

/// <summary>
/// Enables tenant management endpoints.
/// </summary>
[DependsOn(typeof(TenantManagementFeature))]
public class TenantManagementEndpointsFeature(IModule serviceConfiguration) : FeatureBase(serviceConfiguration)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<TenantManagementEndpointsFeature>();
    }
}