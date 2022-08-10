using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Samples.CustomOutcomeActivityConsole.Activities;
using Elsa.Samples.CustomOutcomeActivityConsole.Workflows;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.CustomOutcomeActivityConsole
{
    class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var serviceCollection = new ServiceCollection().AddElsaServices();

            var services = MultitenantContainerFactory.CreateSampleMultitenantContainer(serviceCollection,
                options => options
                    .AddConsoleActivities()
                    .AddActivity<SomeCustomActivity>()
                    .AddWorkflow<DemoWorkflow>());

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Run the workflow.
            await workflowRunner.BuildAndStartWorkflowAsync<DemoWorkflow>();
        }
    }
}