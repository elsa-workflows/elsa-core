using System;
using System.IO;

using Elsa.Activities.Console;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddConsoleActivities(this IServiceCollection services, TextReader? standardIn = default, TextWriter? standardOut = default) =>
            services
                .AddSingleton(standardIn ?? Console.In)
                .AddSingleton(standardOut ?? Console.Out)
                .AddActivity(typeof(ReadLine).Assembly);
    }
}