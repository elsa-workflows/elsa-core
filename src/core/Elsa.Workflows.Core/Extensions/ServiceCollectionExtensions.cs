using Elsa.Expressions;
using Elsa.Options;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElsaCore(this IServiceCollection services, Action<ElsaOptionsConfigurator>? configure = default)
    {
        var configurator = new ElsaOptionsConfigurator(services);
        configure?.Invoke(configurator);
        configurator.ConfigureServices();
        return services;
    }
    
    public static IServiceCollection AddExpressionHandler<THandler, TExpression>(this IServiceCollection services) where THandler : class, IExpressionHandler =>
        services.AddExpressionHandler<THandler>(typeof(TExpression));

    public static IServiceCollection AddExpressionHandler<THandler>(this IServiceCollection services, Type expression) where THandler : class, IExpressionHandler
    {
        // Register handler with DI.
        services.AddSingleton<THandler>();

        // Register handler with options.
        services.Configure<ElsaOptions>(elsa => elsa.RegisterExpressionHandler<THandler>(expression));

        return services;
    }
    
    public static IServiceCollection AddDefaultExpressionHandlers(this IServiceCollection services) =>
        services
            .AddExpressionHandler<LiteralExpressionHandler, LiteralExpression>()
            .AddExpressionHandler<DelegateExpressionHandler, DelegateExpression>()
            .AddExpressionHandler<VariableExpressionHandler, VariableExpression>()
            .AddExpressionHandler<JsonExpressionHandler, JsonExpression>()
            .AddExpressionHandler<OutputExpressionHandler, OutputExpression>()
            .AddExpressionHandler<ElsaExpressionHandler, ElsaExpression>();
}