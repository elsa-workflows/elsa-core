using System;
using System.IO;
using Elsa.Activities.Console;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddConsoleActivities(this IServiceCollection services, TextReader standardIn = default)
        {
            return services
                .AddSingleton(standardIn ?? Console.In)
                .AddActivity<ReadLine>()
                .AddActivity<WriteLine>();
        }
    }
}