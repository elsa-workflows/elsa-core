using Elsa.Common.Features;
using Elsa.Extensions;
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

/// <summary>
/// Provides identity feature to authenticate & authorize API requests.
/// </summary>
[DependsOn(typeof(SystemClockFeature))]
public class IdentityFeature : FeatureBase
{
    /// <inheritdoc />
    public IdentityFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A flag indicating whether a default user should be created. 
    /// </summary>
    public bool CreateDefaultUser { get; set; }

    /// <summary>
    /// A delegate to configure <see cref="Options.IdentityOptions"/>.
    /// </summary>
    public IdentityOptions IdentityOptions { get; set; } = new();

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="IUserStore"/>.
    /// </summary>
    public Func<IServiceProvider, IUserStore> UserStore { get; set; } = sp => sp.GetRequiredService<MemoryUserStore>();
    
    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="IRoleStore"/>.
    /// </summary>
    public Func<IServiceProvider, IRoleStore> RoleStore { get; set; } = sp => sp.GetRequiredService<MemoryRoleStore>();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        if(CreateDefaultUser)
            Module.ConfigureHostedService<SetupDefaultUserHostedService>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure<IdentityOptions>(options => options.CopyFrom(IdentityOptions));
        
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