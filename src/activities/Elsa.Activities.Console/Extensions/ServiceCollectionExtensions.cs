using Elsa.Activities.Console.Activities;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddConsoleActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<ReadLine>()
                .AddActivity<WriteLine>();
        }
    }
}