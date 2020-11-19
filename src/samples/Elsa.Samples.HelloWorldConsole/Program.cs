using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.HelloWorldConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .AddWorkflow<HelloWorld>()
                .AddAutoMapperProfiles<Program>()
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();
            
            // Get a workflow runner.
            var workflowRunner = services.GetService<IWorkflowRunner>();

            // Run the workflow.
            await workflowRunner.RunWorkflowAsync<HelloWorld>();
        }
    }
}