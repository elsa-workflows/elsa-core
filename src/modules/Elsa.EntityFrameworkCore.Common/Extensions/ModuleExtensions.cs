using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Common.Strategies;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Common.Extensions;

/// <summary>
/// Extensions for <see cref="IModule"/> that installs strategies for the <see cref="TenantsFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the strategies for the <see cref="TenantsFeature"/> feature.
    /// </summary>
    public static IModule UseTenantStrategies(this IModule module)
    {
        module.Services.AddScoped<IDbContextStrategy, MustHaveTenantIdBeforeSavingStrategy>();

        return module;
    }
}
