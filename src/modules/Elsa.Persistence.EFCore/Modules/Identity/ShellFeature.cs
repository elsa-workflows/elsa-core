using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Identity;

/// <summary>
/// Base class for identity persistence features.
/// This is not a standalone shell feature - use provider-specific features.
/// </summary>
[UsedImplicitly]
public abstract class EFCoreIdentityPersistenceShellFeatureBase : PersistenceShellFeatureBase<IdentityElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<IUserStore, EFCoreUserStore>();
        services.AddScoped<IApplicationStore, EFCoreApplicationStore>();
        services.AddScoped<IRoleStore, EFCoreRoleStore>();
        AddEntityStore<User, EFCoreUserStore>(services);
        AddEntityStore<Application, EFCoreApplicationStore>(services);
        AddEntityStore<Role, EFCoreRoleStore>(services);
    }
}
