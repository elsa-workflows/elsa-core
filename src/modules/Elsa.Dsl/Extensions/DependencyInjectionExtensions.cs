using Elsa.Dsl.Implementations;
using Elsa.Dsl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDsl(this IServiceCollection services)
    {
        return services
            .AddSingleton<IDslEngine, DslEngine>()
            .AddSingleton<ITypeSystem, TypeSystem>()
            .AddSingleton<IFunctionActivityRegistry, FunctionActivityRegistry>();
    }
}