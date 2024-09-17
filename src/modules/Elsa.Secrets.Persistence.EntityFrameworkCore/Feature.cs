using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Secrets.Management;
using Elsa.Secrets.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore;

/// <summary>
/// Configures the EF Core persistence providers for the secret management feature.
/// </summary>
[DependsOn(typeof(SecretManagementFeature))]
public class EFCoreSecretPersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreSecretPersistenceFeature, SecretsDbContext>(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<SecretManagementFeature>(feature =>
        {
            feature.UseSecretsStore(sp => sp.GetRequiredService<EFCoreSecretStore>());
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddEntityStore<Secret, EFCoreSecretStore>();
    }
}