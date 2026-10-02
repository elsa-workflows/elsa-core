using Elsa.Studio.Alterations.Catalog;
using Elsa.Studio.Alterations.Menu;
using Elsa.Studio.Alterations.Services;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Alterations.Extensions;

/// <summary>
/// Extension methods to register the alterations module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the alterations module: feature, menu, catalog and the per-editor staging service.
    /// </summary>
    public static IServiceCollection AddAlterationsModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, AlterationsMenu>()
            .AddSingleton<IAlterationCatalog, AlterationCatalog>()
            .AddScoped<IAlterationStagingService, AlterationStagingService>();
    }
}
