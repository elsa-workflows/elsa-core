using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Features;
using Elsa.MongoDb.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Identity;

/// <summary>
/// Configures the <see cref="IdentityFeature"/> feature with MongoDB persistence providers.
/// </summary>
[DependsOn(typeof(IdentityFeature))]
[PublicAPI]
public class MongoIdentityPersistenceFeature : PersistenceFeatureBase
{
    /// <inheritdoc />
    public MongoIdentityPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<IdentityFeature>(feature =>
        {
            feature.UserStore = sp => sp.GetRequiredService<MongoUserStore>();
            feature.ApplicationStore = sp => sp.GetRequiredService<MongoApplicationStore>();
            feature.RoleStore = sp => sp.GetRequiredService<MongoRoleStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddCollection<User>("users");
        AddCollection<Application>("applications");
        AddCollection<Role>("roles");

        AddStore<User, MongoUserStore>();
        AddStore<Application, MongoApplicationStore>();
        AddStore<Role, MongoRoleStore>();

        Services.AddHostedService<CreateIndices>();
    }
}