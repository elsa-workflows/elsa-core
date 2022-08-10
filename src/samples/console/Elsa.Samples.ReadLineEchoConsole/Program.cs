using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.ReadLineEchoConsole
{
    class Program
    {
        static async Task Main()
        {
            // Create a service container with Elsa services.
            var serviceCollection = new ServiceCollection().AddElsaServices();

            var services = MultitenantContainerFactory.CreateSampleMultitenantContainer(serviceCollection,
                options => options.AddConsoleActivities());

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