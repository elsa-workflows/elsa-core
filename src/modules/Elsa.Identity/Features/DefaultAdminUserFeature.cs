using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.Features;

/// <summary>
/// Feature that initializes an admin user from configuration if provided.
/// </summary>
[DependsOn(typeof(IdentityFeature))]
[UsedImplicitly]
public class DefaultAdminUserFeature(IModule module) : FeatureBase(module)
{
    public DefaultAdminUserFeature WithAdminUserName(string value)
    {
        Services.Configure<DefaultAdminUserOptions>(options => options.AdminUserName = value);
        return this;
    }

    public DefaultAdminUserFeature WithAdminPassword(string value)
    {
        Services.Configure<DefaultAdminUserOptions>(options => options.AdminPassword = value);
        return this;
    }

    public DefaultAdminUserFeature WithAdminRoleName(string value)
    {
        Services.Configure<DefaultAdminUserOptions>(options => options.AdminRoleName = value);
        return this;
    }

    public DefaultAdminUserFeature WithAdminRolePermissions(params ICollection<string> value)
    {
        Services.Configure<DefaultAdminUserOptions>(options => options.AdminRolePermissions = value);
        return this;
    }

    public override void Apply()
    {
        Services.AddBackgroundTask<AdminUserInitializer>();
    }
}