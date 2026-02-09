using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.ShellFeatures;

/// <summary>
/// Configures multi-tenancy features.
/// </summary>
[ShellFeature(
    DisplayName = "Tenants",
    Description = "Provides multi-tenancy support for workflow isolation",
    DependencyOf = ["Multitenancy"])]
public class TenantsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddScoped<ITenantResolverPipelineInvoker, DefaultTenantResolverPipelineInvoker>()
            .AddScoped<ITenantResolver, DefaultTenantResolver>();
    }
}
