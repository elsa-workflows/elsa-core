using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Features;
using Elsa.Persistence.Dapper.Modules.Identity.Records;
using Elsa.Persistence.Dapper.Modules.Identity.Stores;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.Modules.Identity.Features;

/// <summary>
/// Configures the <see cref="IdentityFeature"/> feature with Dapper persistence providers.
/// </summary>
[DependsOn(typeof(IdentityFeature))]
[DependsOn(typeof(DapperFeature))]
[PublicAPI]
public class DapperIdentityPersistenceFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperIdentityPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<IdentityFeature>(feature =>
        {
            feature.UserStore = sp => sp.GetRequiredService<DapperUserStore>();
            feature.ApplicationStore = sp => sp.GetRequiredService<DapperApplicationStore>();
            feature.RoleStore = sp => sp.GetRequiredService<DapperRoleStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        Services.AddDapperStore<DapperUserStore, UserRecord>("Users");
        Services.AddDapperStore<DapperApplicationStore, ApplicationRecord>("Applications");
        Services.AddDapperStore<DapperRoleStore, RoleRecord>("Roles");
    }
}