using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Features;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.EFCore.Features;

/// <summary>
/// Configures the <see cref="SecretsFeature"/> feature with Entity Framework Core persistence.
/// </summary>
[DependsOn(typeof(SecretsFeature))]
public class EFCoreSecretsPersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreSecretsPersistenceFeature, SecretsElsaDbContext>(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddStore<Secret, EFCoreSecretRepository>();
        Services.AddScoped<ISecretRepository, EFCoreSecretRepository>();
        Services.AddScoped<ISecretManager, DefaultSecretManager>();
        Services.AddScoped<ISecretResolver, DefaultSecretResolver>();
    }
}
