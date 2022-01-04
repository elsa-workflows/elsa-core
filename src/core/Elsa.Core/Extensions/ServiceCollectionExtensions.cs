using Elsa.Contracts;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExpressionHandler<THandler, TExpression>(this IServiceCollection services) where THandler : class, IExpressionHandler =>
        services.AddExpressionHandler<THandler>(typeof(TExpression));

    public static IServiceCollection AddExpressionHandler<THandler>(this IServiceCollection services, Type expression) where THandler : class, IExpressionHandler
    {
        // Register handler with DI.
        services.AddSingleton<THandler>();

        // Register handler with options.
        services.Configure<WorkflowEngineOptions>(elsa => elsa.RegisterExpressionHandler<THandler>(expression));

        return services;
    }
}