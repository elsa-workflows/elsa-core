using Elsa.Extensions;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.ShellFeatures;

/// <summary>
/// Base class for provider-specific AI persistence shell features.
/// </summary>
public abstract class EFCoreAIPersistenceShellFeatureBase : PersistenceShellFeatureBase<AIDbContext>
{
    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AIDbContext>>().CreateDbContext());
        services.AddAIPersistenceStoreServices();
    }
}
