using Elsa.Expressions.Contracts;
using Elsa.Expressions.Features;
using Elsa.Expressions.Options;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/>.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the expressions feature.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="configure">A delegate to configure the <see cref="ExpressionsFeature"/>.</param>
    /// <returns></returns>
    public static IModule UseExpressions(this IModule module, Action<ExpressionsFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }

    /// <summary>
    /// Installs the expressions feature.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <typeparam name="TExpression">The type of the expression.</typeparam>
    /// <returns>The services.</returns>
    public static IServiceCollection AddExpressionHandler<THandler, TExpression>(this IServiceCollection services) where THandler : class, IExpressionHandler =>
        services.AddExpressionHandler<THandler>(typeof(TExpression));

    /// <summary>
    /// Installs the expressions feature.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="expression">The type of the expression.</param>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <returns>The services.</returns>
    public static IServiceCollection AddExpressionHandler<THandler>(this IServiceCollection services, Type expression) where THandler : class, IExpressionHandler
    {
        // Register handler with DI.
        services.TryAddSingleton<THandler>();

        // Register handler with options.
        services.Configure<ExpressionOptions>(elsa => elsa.RegisterExpressionHandler<THandler>(expression));

        return services;
    }
    
    /// <summary>
    /// Register type <see cref="T"/> with the specified alias.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="alias">The alias.</param>
    /// <typeparam name="T">The type.</typeparam>
    /// <returns>The module.</returns>
    public static IModule AddTypeAlias<T>(this IModule module, string alias)
    {
        module.Services.Configure<ExpressionOptions>(options => options.AddTypeAlias<T>(alias));
        return module;
    }
}