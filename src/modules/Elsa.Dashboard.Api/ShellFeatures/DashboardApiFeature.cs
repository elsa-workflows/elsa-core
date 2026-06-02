using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Dashboard.Api.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Api.ShellFeatures;

[ShellFeature(
    DisplayName = "Dashboard API",
    Description = "Provides operational dashboard API endpoints for Elsa Studio",
    DependsOn = ["ElsaFastEndpoints"])]
[UsedImplicitly]
public class DashboardApiFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDashboardApiServices();
    }
}
