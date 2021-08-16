using System.Threading.Tasks;
using Elsa.Samples.RpaWebConsole;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.RpaWebConsole
{
    class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddRpaWebActivities()
                    .AddConsoleActivities()
                    .AddWorkflow<NavigateToWebsite>())
                .BuildServiceProvider();
            
            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Run the workflow.
            //await workflowRunner.BuildAndStartWorkflowAsync<NavigateToWebsite>();
            await workflowRunner.BuildAndStartWorkflowAsync<NavigateToW3School>();
        }
    }
}