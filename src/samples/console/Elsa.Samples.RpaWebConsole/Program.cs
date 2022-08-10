using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.RpaWebConsole
{
    class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var serviceCollection = new ServiceCollection().AddElsaServices();

            var services = MultitenantContainerFactory.CreateSampleMultitenantContainer(serviceCollection,
                options => options
                    .AddRpaWebActivities()
                    .AddConsoleActivities()
                    .AddWorkflow<NavigateToWebsite>());
            
            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Run the workflow.
            //await workflowRunner.BuildAndStartWorkflowAsync<NavigateToWebsite>();
            await workflowRunner.BuildAndStartWorkflowAsync<NavigateToW3School>();
        }
    }
}