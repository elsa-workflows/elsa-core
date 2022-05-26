using Elsa.Expressions.Configuration;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Expressions.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceConfiguration UseExpressions(this IServiceConfiguration configuration, Action<ExpressionsConfigurator>? configure = default)
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