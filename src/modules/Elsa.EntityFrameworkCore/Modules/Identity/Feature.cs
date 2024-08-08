using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Handlers;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Identity;

/// <summary>
/// Configures the <see cref="IdentityFeature"/> feature with Entity Framework Core persistence providers.
/// </summary>
[DependsOn(typeof(IdentityFeature))]
[PublicAPI]
public class EFCoreIdentityPersistenceFeature(IModule module) : PersistenceFeatureBase<IdentityElsaDbContext>(module)
{
    /// Delegate for determining the exception handler.
    public Func<IServiceProvider, IDbExceptionHandler<IdentityElsaDbContext>> DbExceptionHandler { get; set; } = _ => new NoopDbExceptionHandler();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<IdentityFeature>(feature =>
        {
            feature.UserStore = sp => sp.GetRequiredService<EFCoreUserStore>();
            feature.ApplicationStore = sp => sp.GetRequiredService<EFCoreApplicationStore>();
            feature.RoleStore = sp => sp.GetRequiredService<EFCoreRoleStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        Services.AddScoped(DbExceptionHandler);

        AddEntityStore<User, EFCoreUserStore>();
        AddEntityStore<Application, EFCoreApplicationStore>();
        AddEntityStore<Role, EFCoreRoleStore>();
    }
}