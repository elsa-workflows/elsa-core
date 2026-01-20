using CShells.Features;
using Elsa.Identity.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Identity;

/// <summary>
/// Configures the identity feature with Entity Framework Core persistence providers.
/// </summary>
[ShellFeature(
    DisplayName = "EF Core Identity Persistence",
    Description = "Provides Entity Framework Core persistence for identity management",
    DependsOn = ["Identity"])]
[UsedImplicitly]
public class EFCoreIdentityPersistenceShellFeature : PersistenceShellFeatureBase<EFCoreIdentityPersistenceShellFeature, IdentityElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddEntityStore<User, EFCoreUserStore>(services);
        AddEntityStore<Application, EFCoreApplicationStore>(services);
        AddEntityStore<Role, EFCoreRoleStore>(services);
    }
}
