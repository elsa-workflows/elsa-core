using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Sample01.Activities;

namespace Sample01
{
    /// <summary>
    /// A minimal workflows program defined in code with a strongly-typed workflow class.
    /// </summary>
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddActivity<HelloWorld>()
                .AddActivity<GoodByeWorld>()
                .AddProcess<HelloWorldProcess>()
                .BuildServiceProvider();

            // Invoke the workflow.
            var processBuilder = services.GetRequiredService<IProcessBuilder>();
            var process = processBuilder.Build<HelloWorldProcess>();
            var invoker = services.GetService<IProcessRunner>();
            await invoker.RunAsync(process);

            Console.ReadLine();
        }
    }
}