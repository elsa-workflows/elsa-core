using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Workflows;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Samples.ProgrammaticCompositeActivitiesConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a service container with Elsa services.
            var serviceCollection = new ServiceCollection()
                .AddElsaServices()
                .AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning));

            var services = MultitenantContainerFactory.CreateSampleMultitenantContainer(serviceCollection,
                options => options
                    .AddConsoleActivities()
                    .AddActivitiesFrom<Program>()
                    .AddWorkflowsFrom<Program>());

            // Get a workflow host.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Execute the workflow.
            await workflowRunner.BuildAndStartWorkflowAsync<CompositionWorkflow>();
        }
    }
}