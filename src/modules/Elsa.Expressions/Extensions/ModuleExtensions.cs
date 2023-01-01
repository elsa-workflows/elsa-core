using Elsa.Expressions.Features;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseExpressions(this IModule configuration, Action<ExpressionsFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }

    public static IServiceCollection AddExpressionHandler<THandler, TExpression>(this IServiceCollection services) where THandler : class, IExpressionHandler =>
        services.AddExpressionHandler<THandler>(typeof(TExpression));

    public static IServiceCollection AddExpressionHandler<THandler>(this IServiceCollection services, Type expression) where THandler : class, IExpressionHandler
    {
        // Register handler with DI.
        services.TryAddSingleton<THandler>();

        // Register handler with options.
        services.Configure<ExpressionOptions>(elsa => elsa.RegisterExpressionHandler<THandler>(expression));

        return services;
    }
}