using Elsa.Expressions.Contracts;
using Elsa.Expressions.Features;
using Elsa.Expressions.Options;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/>.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the expressions feature.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="configure">A delegate to configure the <see cref="ExpressionsFeature"/>.</param>
    public static IModule UseExpressions(this IModule module, Action<ExpressionsFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }

    /// <summary>
    /// Installs the expressions feature.
    /// </summary>
    /// <param name="services">The services.</param>
    public static IServiceCollection AddExpressionDescriptorProvider<T>(this IServiceCollection services) where T: class, IExpressionDescriptorProvider
    {
        return services.AddSingleton<IExpressionDescriptorProvider, T>();
    }
    
    /// <summary>
    /// Register type <typeparamref name="T"/> with the specified alias.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="alias">The alias.</param>
    /// <typeparam name="T">The type.</typeparam>
    public static IModule AddTypeAlias<T>(this IModule module, string alias)
    {
        module.Services.Configure<ExpressionOptions>(options => options.AddTypeAlias<T>(alias));
        return module;
    }
}