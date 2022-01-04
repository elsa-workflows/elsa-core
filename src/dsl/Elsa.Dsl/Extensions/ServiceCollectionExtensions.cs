using Elsa.Dsl.Abstractions;
using Elsa.Dsl.Contracts;
using Elsa.Dsl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDsl(this IServiceCollection services)
    {
        return services
            .AddSingleton<IDslEngine, DslEngine>()
            .AddSingleton<IFunctionActivityRegistry, FunctionActivityRegistry>();
    }
}