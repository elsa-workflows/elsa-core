using Elsa.Dashboard.Api.Extensions;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Dashboard.Api.Features;

public class DashboardApiFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<DashboardApiFeature>();
    }

    public override void Apply()
    {
        Services.AddDashboardApiServices();
        Module.AddFastEndpointsFromModule();
    }
}
