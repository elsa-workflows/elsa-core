using System.Threading.Tasks;
using Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Activities;
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
            var services = new ServiceCollection()
                .AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning))
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddActivity<NavigateActivity>()
                    .AddActivity<CountdownActivity>()
                    .AddWorkflow<CompositionWorkflow>())
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Get a workflow host.
            var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

            // Execute the workflow.
            await workflowRunner.BuildAndStartWorkflowAsync<CompositionWorkflow>();
        }
    }
}