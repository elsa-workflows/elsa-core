using Elsa.Activities.Console.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Console.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsConsole(this IServiceCollection services)
        {
            return services
                .AddSingleton<IActivityHandler, ReadLineHandler>()
                .AddSingleton<IActivityHandler, WriteLineHandler>();
        }
    }
}