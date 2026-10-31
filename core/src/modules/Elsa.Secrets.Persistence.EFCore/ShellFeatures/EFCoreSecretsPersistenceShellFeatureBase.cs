using Elsa.Persistence.EFCore;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.EFCore.ShellFeatures;

/// <summary>
/// Base class for secrets persistence features.
/// This is not a standalone shell feature - use provider-specific features.
/// </summary>
public abstract class EFCoreSecretsPersistenceShellFeatureBase : PersistenceShellFeatureBase<SecretsElsaDbContext>
{
    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddStore<Secret, EFCoreSecretRepository>(services);
        services.AddScoped<ISecretRepository, EFCoreSecretRepository>();
        services.AddScoped<ISecretManager, DefaultSecretManager>();
        services.AddScoped<ISecretResolver, DefaultSecretResolver>();
    }
}
