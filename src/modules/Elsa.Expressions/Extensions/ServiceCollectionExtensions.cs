using Elsa.Expressions.Options;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTypeAlias<T>(this IServiceCollection services, string alias)
    {
        services.Configure<ExpressionOptions>(options => options.AddTypeAlias<T>(alias));
        return services;
    }
}