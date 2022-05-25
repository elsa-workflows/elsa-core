using Elsa.Expressions.Implementations;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddExpressions(this IServiceCollection services)
    {
        return services
            .AddSingleton<IExpressionEvaluator, ExpressionEvaluator>()
            .AddSingleton<IExpressionHandlerRegistry, ExpressionHandlerRegistry>();
    }

    public static IServiceCollection AddExpressionHandler<THandler, TExpression>(this IServiceCollection services) where THandler : class, IExpressionHandler =>
        services.AddExpressionHandler<THandler>(typeof(TExpression));

    public static IServiceCollection AddExpressionHandler<THandler>(this IServiceCollection services, Type expression) where THandler : class, IExpressionHandler
    {
        // Register handler with DI.
        services.AddSingleton<THandler>();

        // Register handler with options.
        services.Configure<ExpressionOptions>(elsa => elsa.RegisterExpressionHandler<THandler>(expression));

        return services;
    }
}