using Elsa.Persistence.EFCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.ShellFeatures;

/// <summary>
/// Base class for external authentication persistence features.
/// This is not a standalone shell feature - use provider-specific features.
/// </summary>
public abstract class EFCoreExternalAuthenticationPersistenceShellFeatureBase : PersistenceShellFeatureBase<ExternalAuthenticationElsaDbContext>
{
    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddExternalAuthenticationEntityFrameworkCore();
    }
}
