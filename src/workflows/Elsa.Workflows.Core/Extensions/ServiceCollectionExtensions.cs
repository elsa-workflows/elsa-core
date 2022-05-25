using Elsa.Expressions;
using Elsa.Expressions.Extensions;
using Elsa.Expressions.Services;
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

    public static IServiceCollection AddDefaultExpressionHandlers(this IServiceCollection services) =>
        services
            .AddExpressionHandler<LiteralExpressionHandler, LiteralExpression>()
            .AddExpressionHandler<DelegateExpressionHandler, DelegateExpression>()
            .AddExpressionHandler<VariableExpressionHandler, VariableExpression>()
            .AddExpressionHandler<JsonExpressionHandler, JsonExpression>()
            .AddExpressionHandler<OutputExpressionHandler, OutputExpression>()
            .AddExpressionHandler<ElsaExpressionHandler, ElsaExpression>();
}