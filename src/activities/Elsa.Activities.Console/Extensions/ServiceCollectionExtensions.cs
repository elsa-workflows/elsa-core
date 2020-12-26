using System;
using System.IO;
using Elsa;
using Elsa.Activities.Console;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddConsoleActivities(this ElsaOptions options, TextReader? standardIn = default, TextWriter? standardOut = default)
        {
            options.Services
                .AddSingleton(standardIn ?? Console.In)
                .AddSingleton(standardOut ?? Console.Out);
            
            options
                .AddActivity<ReadLine>()
                .AddActivity<WriteLine>();

            return options;
        }
    }
}