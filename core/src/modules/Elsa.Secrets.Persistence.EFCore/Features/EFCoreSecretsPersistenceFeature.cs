using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Features;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        Services.Replace(ServiceDescriptor.Describe(typeof(DefaultSecretManager), typeof(DefaultSecretManager), ServiceLifetime.Scoped));
        Services.Replace(ServiceDescriptor.Scoped<ISecretManager>(sp => sp.GetRequiredService<DefaultSecretManager>()));
        Services.Replace(ServiceDescriptor.Scoped<IManagedSecretManager>(sp => sp.GetRequiredService<DefaultSecretManager>()));
        Services.Replace(ServiceDescriptor.Scoped<ISecretResolver, DefaultSecretResolver>());
    }
}
