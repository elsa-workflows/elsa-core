using System;
using System.IO;
using Elsa.Activities.Console;
using Elsa.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddConsoleActivities(this ElsaOptionsBuilder options, TextReader? standardIn = default, TextWriter? standardOut = default)
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