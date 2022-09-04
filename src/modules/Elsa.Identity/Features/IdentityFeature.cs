using Elsa.Common.Extensions;
using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Implementations;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.Features;

[DependsOn(typeof(SystemClockFeature))]
public class IdentityFeature : FeatureBase
{
    public IdentityFeature(IModule module) : base(module)
    {
    }

    public bool CreateDefaultUser { get; set; }
    public Action<IdentityOptions>? IdentityOptions { get; set; }

    public Func<IServiceProvider, IUserStore> UserStore { get; set; } = sp => sp.GetRequiredService<MemoryUserStore>();
    public Func<IServiceProvider, IRoleStore> RoleStore { get; set; } = sp => sp.GetRequiredService<MemoryRoleStore>();

    public override void ConfigureHostedServices()
    {
        if(CreateDefaultUser)
            Module.ConfigureHostedService<SetupDefaultUserHostedService>();
    }

    public override void Apply()
    {
        Services.Configure(IdentityOptions);

        Services
            .AddMemoryStore<User, MemoryUserStore>()
            .AddMemoryStore<Role, MemoryRoleStore>()
            .AddSingleton(UserStore)
            .AddSingleton(RoleStore)
            .AddSingleton<IPasswordHasher, DefaultPasswordHasher>()
            .AddSingleton<IAccessTokenIssuer, DefaultAccessTokenIssuer>()
            .AddSingleton<ICredentialsValidator, DefaultCredentialsValidator>()
            ;
    }
}