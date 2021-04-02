using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.ReadLineEchoConsole
{
    class Program
    {
        static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddConsoleActivities())
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Build a new workflow.
            var workflow = services.GetRequiredService<IWorkflowBuilder>()
                .WriteLine("What's your name?")
                .ReadLine()
                .WriteLine(context => $"Greetings, {context.Input}!")
                .Build();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IStartsWorkflow>();

            // Execute the workflow.
            await workflowRunner.StartWorkflowAsync(workflow);
        }
    }
}