using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

public class TenantResolverFeature(IModule module) : FeatureBase(module)
{
    public Func<IServiceProvider, ITenantResolver> TenantResolver { get; set; } = sp => sp.GetRequiredService<DefaultTenantResolver>();
    
    public override void Apply()
    {
        Services.AddTransient<DefaultTenantResolver>();
        Services.AddScoped(TenantResolver);
    }
}