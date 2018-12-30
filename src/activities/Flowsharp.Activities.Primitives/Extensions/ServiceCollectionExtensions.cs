using Flowsharp.Activities.Primitives.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Flowsharp.Activities.Primitives.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsPrimitives(this IServiceCollection services)
        {
            return services
                .AddSingleton<IActivityHandler, SetVariableHandler>()
                .AddSingleton<IActivityHandler, ForEachHandler>()
                .AddSingleton<IActivityHandler, IfElseHandler>();
        }
    }
}