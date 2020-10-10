using System;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Provider.Sqlite;

namespace Elsa.Samples.HelloWorldConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection() 
                //.AddElsa(options => options.UsePersistence(config => config.UseSqLite("Data Source=elsa.db;Cache=Shared")))
                .AddElsa()
                .AddConsoleActivities()
                .AddSingleton(Console.In)
                .BuildServiceProvider();
            
            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();
            
            // Build a new workflow.
            var workflow = services.GetService<IWorkflowBuilder>()
                .WriteLine("Hello World!")
                .WriteLine("What's your name?")
                .ReadLine()
                .WriteLine(context => $"Greetings, {context.Input}!")
                .Build();
            
            // Get the workflow host.
            var workflowHost = services.GetService<IWorkflowHost>();

            // Execute the workflow.
            await workflowHost.RunWorkflowAsync(workflow);
        }
    }
}